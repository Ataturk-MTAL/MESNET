using MESNET.Common.Shared.Pagination;

namespace MESNET.Enrollment.Application.Queries;

public sealed record ListTeachers(Guid? InstitutionId, Guid? AcademicPeriodId) : PagedQuery;
