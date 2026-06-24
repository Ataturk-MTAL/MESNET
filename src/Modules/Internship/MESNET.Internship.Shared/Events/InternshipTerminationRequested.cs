namespace MESNET.Internship.Shared.Events;

public sealed record InternshipTerminationRequested(
    Guid InternshipId,
    string Reason,
    string ReasonType,
    string RequestedBy);
