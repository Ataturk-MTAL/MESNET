using Marten;
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

    private async Task GenerateMonthlyReports(CancellationToken ct)
    {
        _logger.LogInformation("Aylık rapor üretimi başlıyor (Form 7 + Form 2)...");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var session = scope.ServiceProvider.GetRequiredService<IDocumentStore>().QuerySession();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        try
        {
            var now = DateTime.UtcNow;
            var year = now.Year;
            var month = now.Month;
            var academicYear = $"{year - 1} / {year}";
            var systemUser = new UserContext(Guid.Empty, "Sistem (Otomatik Rapor)");

            // Aktif placement'ı olan tüm kayıtları al
            var placements = await session.Query<StudentPlacementReportView>()
                .Where(p => p.BusinessId != Guid.Empty)
                .ToListAsync(ct);

            // ─── Form 7: Aylık Devamsızlık Raporu (işletme bazlı) ───
            var businessGroups = placements
                .GroupBy(p => new { p.InstitutionId, p.AcademicPeriodId, p.BusinessId })
                .ToList();

            _logger.LogInformation("Form 7: {Count} işletme grubu için rapor üretilecek", businessGroups.Count);

            var form7Generated = 0;
            foreach (var group in businessGroups)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    var data = await GenerateMonthlyAttendanceReportHandler.BuildReportData(
                        session,
                        group.Key.InstitutionId,
                        group.Key.AcademicPeriodId,
                        group.Key.BusinessId,
                        year, month,
                        "", // InstitutionName — scheduler'da bilinmiyor
                        academicYear);

                    var command = new GenerateMonthlyAttendanceDocument(data, systemUser);
                    await bus.InvokeAsync(command, ct);
                    form7Generated++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Form 7 üretim hatası — BusinessId: {BusinessId}, InstitutionId: {InstitutionId}",
                        group.Key.BusinessId, group.Key.InstitutionId);
                }
            }

            _logger.LogInformation("Form 7 üretimi tamamlandı — {Count}/{Total} başarılı",
                form7Generated, businessGroups.Count);

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
