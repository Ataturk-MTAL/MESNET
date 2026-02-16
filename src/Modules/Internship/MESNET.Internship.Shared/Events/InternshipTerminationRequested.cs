namespace MESNET.Internship.Shared.Events;

public sealed record InternshipTerminationRequested(
    Guid InternshipId,
    Guid StudentId,
    string Reason,
    string ReasonType,
    string RequestedBy);
