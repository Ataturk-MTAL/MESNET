namespace MESNET.Internship.Shared.Events;

public sealed record TerminationFormRequested(
    Guid InternshipId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId);
