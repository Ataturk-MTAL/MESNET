namespace MESNET.Enrollment.Shared.Events;

public sealed record InternshipApplied(
    Guid StudentId,
    Guid? BusinessId,
    string BranchCode,
    string Source);
