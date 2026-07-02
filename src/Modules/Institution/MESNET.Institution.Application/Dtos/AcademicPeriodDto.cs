namespace MESNET.Institution.Application.Dtos;

public sealed record AcademicPeriodDto(
    Guid Id,
    Guid InstitutionId,
    string Name,
    int StartYear,
    int EndYear,
    string StartDate,
    string EndDate,
    string Status,
    string StatusSlug,
    string CreatedAt,
    string? ClosedAt,
    string? GradeEntryStartDate,
    string? GradeEntryEndDate);
