using Marten;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Services;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.ReadModels;
using MESNET.Payment.Core.Services;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MESNET.Payment.Application.Handlers;

/// <summary>
/// Aktif dönemde, hesaplanan ayla kesişen her sözleşme için maaş dönemi açar (#63, #154).
/// </summary>
/// <remarks>
/// <para><b>Yerleştirme değil sözleşme taranır (#154).</b> Eski hâli yalnız <c>IsActive</c>
/// yerleştirmeleri tarıyordu ve ay ortasında feshedilen yerleştirme o an kapalı olduğu için
/// atlanıyordu: <b>ayrılınan işletme için maaş dönemi hiç açılmıyordu</b> — öğrenci orada fiilen
/// çalıştığı günlerin ücretini alamıyordu. Fesih sonrası yeniden yerleştirilmemiş öğrencide ise
/// o ay hiç dönem açılmıyor, ayın tamamı kayboluyordu.</para>
///
/// <para>Artık ölçüt "şu anda aktif mi" değil <b>"bu ayla kesişiyor mu"</b>: ay ortası fesihte
/// eski sözleşme için de yeni sözleşme için de ayrı dönem açılır, her biri yalnız kendi
/// istihdam günlerini kapsar.</para>
/// </remarks>
public static class OpenMonthlySalaryPeriodsHandler
{
    public static async Task<OpenMonthlySalaryPeriodsResult> Handle(
        OpenMonthlySalaryPeriods command,
        IQuerySession session,
        IMessageBus bus,
        ILogger<OpenMonthlySalaryPeriodsResult> logger)
    {
        // Kapalı dönemde yazma yapılmaz (CLAUDE.md — geçmiş dönem salt okunur).
        var activePeriodIds = await session.Query<AcademicPeriodView>()
            .Where(p => p.IsActive)
            .Select(p => p.Id)
            .ToListAsync();

        if (activePeriodIds.Count == 0)
        {
            logger.LogInformation("Aktif akademik dönem yok, maaş dönemi açılmadı: {Month}", command.Month);
            return new OpenMonthlySalaryPeriodsResult(command.Month, 0, 0, 0);
        }

        if (!SalaryMonth.TryParse(command.Month, out var year, out var month))
        {
            logger.LogWarning("Geçersiz ay biçimi, maaş dönemi açılmadı: {Month}", command.Month);
            return new OpenMonthlySalaryPeriodsResult(command.Month, 0, 0, 0);
        }

        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = new DateTime(
            year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Utc);

        // Ayla kesişen sözleşmeler: ay bitmeden başlamış ve ay başlamadan kapanmamış olanlar.
        // Taslak sözleşme dışarıda — imza tamamlanmadan istihdam başlamaz.
        var contracts = await session.Query<ContractEmploymentView>()
            .Where(c => c.IsActivated
                     && activePeriodIds.Contains(c.AcademicPeriodId)
                     && c.StartDate <= monthEnd
                     && (c.EndDate == null || c.EndDate >= monthStart))
            .ToListAsync();

        var opened = 0;
        var skipped = 0;

        foreach (var contract in contracts)
        {
            // Kesişme gün sayısı sıfırsa (ör. sözleşme ayın son gününden sonra başlamış ve
            // saat bileşeni yüzünden sorguya takılmış) dönem açılmaz — sıfır ücretli kayıt
            // dekont yükümlülüğü ve gecikme uyarısı doğururdu.
            var employedDays = EmploymentDays.InMonth(contract.StartDate, contract.EndDate, year, month);
            if (employedDays == 0) { skipped++; continue; }

            var salaryPeriodId = SalaryPeriodId.For(contract.Id, command.Month);

            // Önceki bir koşu veya elle tetikleme zaten açmış olabilir — tekrar çalıştırmak güvenli.
            var existing = await session.LoadAsync<PaymentSummary>(salaryPeriodId);
            if (existing is not null) { skipped++; continue; }

            await bus.PublishAsync(new CalculateMonthlySalary(
                salaryPeriodId,
                contract.Id,
                contract.StudentId,
                contract.BusinessId,
                contract.InstitutionId,
                contract.AcademicPeriodId,
                command.Month,
                command.ReferenceDate));

            opened++;
        }

        logger.LogInformation(
            "Maaş dönemi açma tamamlandı: {Month} — {Opened} açıldı, {Skipped} atlandı, {Total} kesişen sözleşme",
            command.Month, opened, skipped, contracts.Count);

        return new OpenMonthlySalaryPeriodsResult(command.Month, opened, skipped, contracts.Count);
    }
}
