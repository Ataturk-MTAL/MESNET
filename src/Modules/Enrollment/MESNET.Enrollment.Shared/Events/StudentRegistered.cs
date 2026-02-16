namespace MESNET.Enrollment.Shared.Events;

public sealed record StudentRegistered(
    Guid StudentId,
    string FullName,
    Guid InstitutionId,
    string BranchCode);
