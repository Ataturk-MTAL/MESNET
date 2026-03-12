namespace MESNET.Coordination.Application.Commands;

public sealed record DeleteWeeklyVisitPlan(
    Guid PlanId,
    Guid InstitutionId);
