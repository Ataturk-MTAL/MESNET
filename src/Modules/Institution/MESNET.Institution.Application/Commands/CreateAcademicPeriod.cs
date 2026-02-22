namespace MESNET.Institution.Application.Commands;

public sealed record CreateAcademicPeriod(
    Guid InstitutionId,
    string Name,
    int StartYear,
    int EndYear,
    DateOnly StartDate,
    DateOnly EndDate);
