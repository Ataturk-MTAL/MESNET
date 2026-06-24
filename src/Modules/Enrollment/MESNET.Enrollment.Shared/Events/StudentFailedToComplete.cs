namespace MESNET.Enrollment.Shared.Events;

public sealed record StudentFailedToComplete(
    Guid PlacementId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    string BranchCode);
