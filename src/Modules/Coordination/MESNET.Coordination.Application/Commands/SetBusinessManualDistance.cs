namespace MESNET.Coordination.Application.Commands;

public sealed record SetBusinessManualDistance(
    Guid BusinessId,
    double DistanceKm,
    Guid InstitutionId);
