namespace MESNET.Internship.Application.Commands;

public sealed record OverrideTerminationApproval(
    Guid InternshipId,
    string OverriddenBy,
    string Reason);
