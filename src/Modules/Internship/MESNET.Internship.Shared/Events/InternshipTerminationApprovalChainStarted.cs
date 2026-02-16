namespace MESNET.Internship.Shared.Events;

public sealed record InternshipTerminationApprovalChainStarted(
    Guid InternshipId,
    Guid StudentId,
    bool RequiresParentApproval);
