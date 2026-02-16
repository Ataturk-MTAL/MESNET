namespace MESNET.Internship.Shared.Events;

public sealed record InternshipReplacementRequested(
    Guid StudentId,
    Guid OldBusinessId,
    Guid InstitutionId,
    string BranchCode);
