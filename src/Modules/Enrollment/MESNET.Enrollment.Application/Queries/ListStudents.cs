using MESNET.Common.Shared.Pagination;

namespace MESNET.Enrollment.Application.Queries;

public sealed record ListStudents(
    Guid? InstitutionId,
    Guid? AcademicPeriodId,
    string? BranchCode,
    string? Section,
    string? Status) : PagedQuery;
