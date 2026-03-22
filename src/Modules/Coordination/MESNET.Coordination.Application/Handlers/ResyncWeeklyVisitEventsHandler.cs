using Marten;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Shared.Events;
using Wolverine;

namespace MESNET.Coordination.Application.Handlers;

/// <summary>
/// Mevcut haftalık ziyaret planlarını okuyup WeeklyVisitsGenerated event'ini tekrar yayınlar.
/// Reporting modülünün VisitAssignmentReportView'ını sıfırdan doldurmasını sağlar.
/// Schema sıfırlanması veya consumer hatası sonrası kullanılır.
/// </summary>
public static class ResyncWeeklyVisitEventsHandler
{
    public static async Task<int> Handle(
        ResyncWeeklyVisitEvents command,
        IQuerySession session,
        IMessageBus bus,
        CancellationToken ct)
    {
        // Bu dönemdeki tüm planları getir
        var plans = await session.Query<WeeklyVisitPlan>()
            .Where(p => p.InstitutionId == command.InstitutionId
                     && p.AcademicPeriodId == command.AcademicPeriodId)
            .ToListAsync(ct);

        if (plans.Count == 0) return 0;

        var totalAssignments = 0;

        foreach (var plan in plans)
        {
            // Bu plana ait atamaları getir
            var assignments = await session.Query<WeeklyVisitAssignment>()
                .Where(a => a.PlanId == plan.Id)
                .ToListAsync(ct);

            if (assignments.Count == 0) continue;

            var @event = new WeeklyVisitsGenerated(
                plan.Id,
                plan.InstitutionId,
                plan.AcademicPeriodId,
                assignments.Select(a => new WeeklyVisitAssignmentInfo(
                    a.Id, a.TeacherId, a.TeacherName,
                    a.BusinessId, a.BusinessName,
                    null, // address — resync'te gerekli değil
                    a.BranchCode, a.BranchName,
                    a.VisitDate, a.PeriodCount)).ToList());

            await bus.PublishAsync(@event);
            totalAssignments += assignments.Count;
        }

        return totalAssignments;
    }
}
