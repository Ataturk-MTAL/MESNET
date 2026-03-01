namespace MESNET.Coordination.Application.Queries;

public sealed record GetCoordinationSummary(
    Guid InstitutionId,
    string? BranchCode = null);
