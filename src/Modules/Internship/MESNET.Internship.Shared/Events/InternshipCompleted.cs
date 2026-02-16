namespace MESNET.Internship.Shared.Events;

public sealed record InternshipCompleted(
    Guid InternshipId,
    Guid StudentId,
    Guid BusinessId,
    DateTime CompletedAt);
