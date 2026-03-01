using MESNET.Common.Shared.Pagination;

namespace MESNET.Coordination.Application.Queries;

public sealed record ListSkillExams(
    Guid? StudentId,
    Guid? BusinessId,
    Guid? AcademicPeriodId,
    int? AcademicYear,
    string? Semester) : PagedQuery;
