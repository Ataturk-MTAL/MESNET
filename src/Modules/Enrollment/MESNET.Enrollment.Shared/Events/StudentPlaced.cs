namespace MESNET.Enrollment.Shared.Events;

public sealed record StudentPlaced(
    Guid PlacementId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    Guid? TeacherId,
    DateTime PlacedAt,
    string StudentName = "",
    string BusinessName = "",
    string BranchCode = "",
    string BranchName = "");
