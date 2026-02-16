namespace MESNET.Internship.Shared.Events;

public sealed record InternshipStarted(
    Guid InternshipId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    DateTime StartedAt);
