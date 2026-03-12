using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class DeleteWeeklyVisitPlanHandler
{
    public static async Task Handle(
        DeleteWeeklyVisitPlan command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var plan = await session.LoadAsync<WeeklyVisitPlan>(command.PlanId, ct);
        if (plan is null)
            throw new DomainException(CoordinationErrors.WeeklyVisitPlanNotFound(command.PlanId));

        if (plan.InstitutionId != command.InstitutionId)
            throw new DomainException(CoordinationErrors.WeeklyVisitPlanNotFound(command.PlanId));

        // Dönem kontrolü
        var period = await session.LoadAsync<AcademicPeriodView>(plan.AcademicPeriodId, ct);
        if (period is not null && !period.IsActive)
            throw new DomainException(CoordinationErrors.AcademicPeriodClosed(plan.AcademicPeriodId));

        // Önce atama kayıtlarını sil
        session.DeleteWhere<WeeklyVisitAssignment>(a => a.PlanId == command.PlanId);

        // Planı sil
        session.Delete<WeeklyVisitPlan>(command.PlanId);

        await session.SaveChangesAsync(ct);
    }
}
