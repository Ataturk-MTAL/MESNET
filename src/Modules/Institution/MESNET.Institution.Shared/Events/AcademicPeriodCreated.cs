namespace MESNET.Institution.Shared.Events;

public sealed record AcademicPeriodCreated(
    Guid AcademicPeriodId,
    Guid InstitutionId,
    string Name,
    int StartYear,
    int EndYear,
    DateOnly StartDate,
    DateOnly EndDate);
