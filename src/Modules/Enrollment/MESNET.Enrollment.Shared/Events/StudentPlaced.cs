namespace MESNET.Enrollment.Shared.Events;

public sealed record StudentPlaced(
    Guid PlacementId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    DateTime PlacedAt);
