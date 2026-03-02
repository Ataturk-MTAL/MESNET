namespace MESNET.Coordination.Application.Queries;

public sealed record GetTeacherOverview(
    Guid TeacherId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    string Semester);
