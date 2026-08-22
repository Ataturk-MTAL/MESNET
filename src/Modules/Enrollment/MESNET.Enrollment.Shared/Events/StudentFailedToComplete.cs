namespace MESNET.Enrollment.Shared.Events;

public sealed record StudentFailedToComplete(
    Guid PlacementId,
    Guid StudentId,
    // Okulda stajda null (#159).
    Guid? BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    string BranchCode);
