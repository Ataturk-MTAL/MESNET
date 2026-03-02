namespace MESNET.Coordination.Application.Queries;

public sealed record GetAllTeachersOverview(
    Guid InstitutionId,
    Guid AcademicPeriodId,
    string Semester,
    string? BranchCode = null);
