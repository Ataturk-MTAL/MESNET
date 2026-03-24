using MESNET.Common.Shared.Pagination;

namespace MESNET.Enrollment.Application.Queries;

public sealed record ListPlacements(
    Guid? BusinessId,
    Guid? StudentId,
    Guid? AcademicPeriodId,
    string? Status,
    Guid? InstitutionId = null,
    Guid? TeacherId = null,
    string? BranchCode = null) : PagedQuery;
