namespace MESNET.Institution.Shared.Events;

public sealed record AcademicPeriodClosed(
    Guid AcademicPeriodId,
    Guid InstitutionId,
    DateTime ClosedAt);
