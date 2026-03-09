namespace MESNET.Coordination.Application.Queries;

public sealed record GetAssignmentHistory(
    Guid BusinessId,
    Guid InstitutionId);
