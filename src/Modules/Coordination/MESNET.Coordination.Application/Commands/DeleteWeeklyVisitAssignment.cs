namespace MESNET.Coordination.Application.Commands;

public sealed record DeleteWeeklyVisitAssignment(
    Guid PlanId,
    Guid AssignmentId,
    Guid InstitutionId);
