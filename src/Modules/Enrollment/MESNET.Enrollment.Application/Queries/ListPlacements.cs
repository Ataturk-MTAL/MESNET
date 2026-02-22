namespace MESNET.Enrollment.Application.Queries;

public sealed record ListPlacements(
    Guid? BusinessId,
    Guid? StudentId,
    Guid? AcademicPeriodId,
    string? Status);
