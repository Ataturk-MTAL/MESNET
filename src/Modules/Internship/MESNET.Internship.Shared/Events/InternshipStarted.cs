namespace MESNET.Internship.Shared.Events;

public sealed record InternshipStarted(
    Guid InternshipId,
    Guid PlacementId,
    Guid StudentId,
    string StudentName,
    Guid BusinessId,
    string BusinessName,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    DateTime StartedAt);
