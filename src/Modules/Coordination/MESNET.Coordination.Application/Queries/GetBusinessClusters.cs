namespace MESNET.Coordination.Application.Queries;

public sealed record GetBusinessClusters(
    Guid InstitutionId,
    Guid AcademicPeriodId,
    double EpsMeters = 500,
    int MinPoints = 3);
