namespace MESNET.Coordination.Application.Queries;

public sealed record GetBusinessClusters(
    Guid InstitutionId,
    double EpsMeters = 1000,
    int MinPoints = 3,
    string? BranchCode = null);
