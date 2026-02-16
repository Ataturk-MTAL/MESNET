namespace MESNET.Internship.Application.Commands;

public sealed record RequestTermination(
    Guid InternshipId,
    string Reason,
    string ReasonType,
    string RequestedBy);
