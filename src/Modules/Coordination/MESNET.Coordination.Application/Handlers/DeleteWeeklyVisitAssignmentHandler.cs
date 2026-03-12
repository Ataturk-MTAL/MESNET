using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class DeleteWeeklyVisitAssignmentHandler
{
    public static async Task Handle(
        DeleteWeeklyVisitAssignment command,
        IDocumentSession session,
        CancellationToken ct)
    {
        // Plan kontrolü
        var plan = await session.LoadAsync<WeeklyVisitPlan>(command.PlanId, ct);
        if (plan is null)
            throw new DomainException(CoordinationErrors.WeeklyVisitPlanNotFound(command.PlanId));

        if (plan.InstitutionId != command.InstitutionId)
            throw new DomainException(CoordinationErrors.WeeklyVisitPlanNotFound(command.PlanId));

        // Dönem kontrolü
        var period = await session.LoadAsync<AcademicPeriodView>(plan.AcademicPeriodId, ct);
        if (period is not null && !period.IsActive)
            throw new DomainException(CoordinationErrors.AcademicPeriodClosed(plan.AcademicPeriodId));

        // Atama kontrolü
        var assignment = await session.LoadAsync<WeeklyVisitAssignment>(command.AssignmentId, ct);
        if (assignment is null || assignment.PlanId != command.PlanId)
            throw new DomainException(CoordinationErrors.WeeklyVisitAssignmentNotFound(command.AssignmentId));

        // Sil + plan sayısını güncelle
        session.Delete<WeeklyVisitAssignment>(command.AssignmentId);
        plan.AssignmentCount = Math.Max(0, plan.AssignmentCount - 1);
        session.Store(plan);

        await session.SaveChangesAsync(ct);
    }
}
