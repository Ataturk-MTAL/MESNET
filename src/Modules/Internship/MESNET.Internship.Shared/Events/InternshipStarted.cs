namespace MESNET.Internship.Shared.Events;

public sealed record InternshipStarted(
    Guid InternshipId,
    Guid PlacementId,
    Guid StudentId,
    string StudentName,
    // Okulda stajda null (#159) — işveren yok.
    Guid? BusinessId,
    string BusinessName,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    DateTime StartedAt);
