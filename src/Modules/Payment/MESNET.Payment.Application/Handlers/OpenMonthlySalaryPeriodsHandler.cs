using Marten;
using MESNET.Payment.Application.Commands;
using MESNET.Payment.Application.Services;
using MESNET.Payment.Core.Entities;
using MESNET.Payment.Core.ReadModels;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace MESNET.Payment.Application.Handlers;

/// <summary>
/// Aktif dönemdeki her aktif yerleştirme için maaş dönemi açar (#63).
/// </summary>
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

        var placements = await session.Query<PlacementView>()
            .Where(p => p.IsActive && activePeriodIds.Contains(p.AcademicPeriodId))
            .ToListAsync();

        var opened = 0;
        var skipped = 0;

        foreach (var placement in placements)
        {
            var salaryPeriodId = SalaryPeriodId.For(placement.StudentId, command.Month);

            // Önceki bir koşu veya elle tetikleme zaten açmış olabilir — tekrar çalıştırmak güvenli.
            var existing = await session.LoadAsync<PaymentSummary>(salaryPeriodId);
            if (existing is not null) { skipped++; continue; }

            await bus.PublishAsync(new CalculateMonthlySalary(
                salaryPeriodId,
                placement.StudentId,
                placement.BusinessId,
                placement.InstitutionId,
                placement.AcademicPeriodId,
                command.Month,
                command.ReferenceDate));

            opened++;
        }

        logger.LogInformation(
            "Maaş dönemi açma tamamlandı: {Month} — {Opened} açıldı, {Skipped} zaten vardı, {Total} aktif yerleştirme",
            command.Month, opened, skipped, placements.Count);

        return new OpenMonthlySalaryPeriodsResult(command.Month, opened, skipped, placements.Count);
    }
}
