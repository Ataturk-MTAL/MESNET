namespace MESNET.Internship.Shared.Events;

public sealed record TerminationApprovalOverridden(
    Guid InternshipId,
    Guid StudentId,
    string OverriddenBy,
    string Reason,
    DateTime OverriddenAt);
