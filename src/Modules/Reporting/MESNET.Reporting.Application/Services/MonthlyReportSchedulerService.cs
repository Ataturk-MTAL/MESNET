using Marten;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Reporting.Application.Commands;
using MESNET.Reporting.Application.Handlers;
using MESNET.Reporting.Core.Models;
using MESNET.Reporting.Core.ReadModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MESNET.Reporting.Application.Services;

/// <summary>
/// Her ayın son günü gece yarısı (00:30) tüm aktif işletmeler için
/// aylık devamsızlık raporu (Form 7) ve aylık eğitim faaliyeti formu (Form 2) üretir ve MinIO'ya arşivler.
/// </summary>
public class MonthlyReportSchedulerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonthlyReportSchedulerService> _logger;

    public MonthlyReportSchedulerService(
        IServiceScopeFactory scopeFactory,
        ILogger<MonthlyReportSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = CalculateNextRun(now);
            var delay = nextRun - now;

            _logger.LogInformation(
                "Aylık rapor scheduler — sonraki çalışma: {NextRun:yyyy-MM-dd HH:mm} UTC ({Delay})",
                nextRun, delay);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            await GenerateMonthlyReports(stoppingToken);
        }
    }

    /// <summary>
    /// Her ayın son günü 00:30 UTC'de çalışır.
    /// </summary>
    private static DateTime CalculateNextRun(DateTime now)
    {
        var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
        var lastDayOfMonth = new DateTime(now.Year, now.Month, daysInMonth, 0, 30, 0, DateTimeKind.Utc);

        // Eğer bu ayın son gününü geçtiyse, sonraki ayın son gününe git
        if (now >= lastDayOfMonth)
        {
            var nextMonth = now.AddMonths(1);
            var nextDaysInMonth = DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month);
            return new DateTime(nextMonth.Year, nextMonth.Month, nextDaysInMonth, 0, 30, 0, DateTimeKind.Utc);
        }

        return lastDayOfMonth;
    }

    /// <summary>
    /// Rapor üretimi <b>her kiracı için ayrı ayrı</b> çalışır (#149).
    ///
    /// <para>Zamanlanmış iş bir isteğe bağlı olmadığı için kiracıyı devralamaz; kiracılık
    /// açıldıktan sonra kiracısız session <c>DefaultTenantUsageDisabledException</c> fırlatır.
    /// Bir okulun raporu patlarsa diğerleri üretilmeye devam eder — tek okul yüzünden bütün
    /// ayın raporlarını kaybetmek, o okulun raporunu kaybetmekten kötüdür.</para>
    /// </summary>
    private async Task GenerateMonthlyReports(CancellationToken ct)
    {
        await using var directoryScope = _scopeFactory.CreateAsyncScope();
        var tenants = await directoryScope.ServiceProvider
            .GetRequiredService<ITenantDirectory>()
            .GetActiveTenantsAsync(ct);

        if (tenants.Count == 0)
        {
            _logger.LogWarning("Aylık rapor üretimi atlandı — kayıtlı kiracı yok.");
            return;
        }

        foreach (var tenantId in tenants)
        {
            try
            {
                await GenerateMonthlyReportsForTenant(tenantId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Aylık rapor üretimi başarısız — Kiracı: {TenantId}", tenantId);
            }
        }
    }

    private async Task GenerateMonthlyReportsForTenant(string tenantId, CancellationToken ct)
    {
        _logger.LogInformation(
            "Aylık rapor üretimi başlıyor (Form 7 + Form 2) — Kiracı: {TenantId}", tenantId);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var session = scope.ServiceProvider.GetRequiredService<IDocumentStore>().QuerySession(tenantId);
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        // Handler'ların açtığı session'lar kiracıyı buradan devralır; komutları tek tek
        // etiketlemek, her yeni komutun bunu hatırlamasını gerektirirdi.
        bus.TenantId = tenantId;

        try
        {
            var now = DateTime.UtcNow;
            var year = now.Year;
            var month = now.Month;
            var academicYear = AcademicYear.Format(year - 1, year);
            var systemUser = new UserContext(Guid.Empty, "Sistem (Otomatik Rapor)");

            // Aktif placement'ı olan tüm kayıtları al
            var placements = await session.Query<StudentPlacementReportView>()
                .Where(p => p.BusinessId != Guid.Empty)
                .ToListAsync(ct);

            // ─── Form 7: Aylık Devamsızlık Raporu (öğretmen bazlı toplu) ───
            // Her öğretmen için: tüm işletmeler → tek PDF (her işletme = 1 sayfa)
            var teacherGroups = placements
                .Where(p => p.TeacherId.HasValue)
                .GroupBy(p => new { p.InstitutionId, p.AcademicPeriodId, TeacherId = p.TeacherId!.Value })
                .ToList();

            _logger.LogInformation("Form 7: {Count} öğretmen grubu için rapor üretilecek", teacherGroups.Count);

            var form7Generated = 0;
            foreach (var teacherGroup in teacherGroups)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    // Okulda staj yerleştirmesi işletme taşımaz (#159) — işletme başına
                    // sayfalanan bu rapora girmez.
                    var businessIds = teacherGroup
                        .Where(p => p.BusinessId.HasValue)
                        .Select(p => p.BusinessId!.Value)
                        .Distinct()
                        .ToList();

                    var pages = new List<MonthlyAttendanceReportData>();
                    foreach (var businessId in businessIds)
                    {
                        var pageData = await GenerateMonthlyAttendanceReportHandler.BuildReportData(
                            session,
                            teacherGroup.Key.InstitutionId,
                            teacherGroup.Key.AcademicPeriodId,
                            businessId,
                            year, month,
                            "", // InstitutionName — scheduler'da bilinmiyor
                            academicYear);
                        pages.Add(pageData);
                    }

                    if (pages.Count == 0) continue;

                    var teacherName = teacherGroup.First().TeacherName;
                    var batchCommand = new GenerateMonthlyAttendanceBatchDocument(
                        pages, systemUser,
                        InstitutionId: teacherGroup.Key.InstitutionId,
                        TeacherId: teacherGroup.Key.TeacherId,
                        Description: $"{teacherName} — {pages.Count} işletme, {month}/{year}");

                    await bus.InvokeAsync(batchCommand, ct);
                    form7Generated++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Form 7 üretim hatası — TeacherId: {TeacherId}, InstitutionId: {InstitutionId}",
                        teacherGroup.Key.TeacherId, teacherGroup.Key.InstitutionId);
                }
            }

            _logger.LogInformation("Form 7 üretimi tamamlandı — {Count}/{Total} başarılı",
                form7Generated, teacherGroups.Count);

            // ─── Form 2: Aylık Eğitim Faaliyeti Formu (öğrenci bazlı) ───
            _logger.LogInformation("Form 2: {Count} öğrenci için rapor üretilecek", placements.Count);

            var form2Generated = 0;
            foreach (var placement in placements)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    var formData = new MonthlyActivityFormData
                    {
                        StudentId = placement.StudentId,
                        BusinessId = placement.BusinessId,
                        InstitutionId = placement.InstitutionId,
                        TeacherId = placement.TeacherId,
                        InstitutionName = "", // Scheduler'da bilinmiyor
                        StudentFullName = placement.StudentName,
                        StudentNumber = placement.StudentNumber,
                        BranchName = placement.BranchName,
                        BusinessName = placement.BusinessName,
                        ClassYear = placement.ClassYear,
                        AcademicYear = academicYear,
                        Year = year,
                        Month = month,
                        Activities = [], // Boş — öğretmen yazdırdıktan sonra elle doldurur
                        MasterInstructorName = placement.BusinessContactName ?? "",
                        CoordinatorTeacherName = "" // Scheduler'da öğretmen adı bilinmiyor
                    };

                    var command = new GenerateMonthlyActivityDocument(formData, systemUser);
                    await bus.InvokeAsync(command, ct);
                    form2Generated++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Form 2 üretim hatası — StudentId: {StudentId}, BusinessId: {BusinessId}",
                        placement.StudentId, placement.BusinessId);
                }
            }

            _logger.LogInformation("Form 2 üretimi tamamlandı — {Count}/{Total} başarılı",
                form2Generated, placements.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aylık rapor scheduler hatası");
        }
        finally
        {
            await session.DisposeAsync();
        }
    }
}
