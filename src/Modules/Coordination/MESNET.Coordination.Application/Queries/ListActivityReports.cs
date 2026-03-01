using MESNET.Common.Shared.Pagination;

namespace MESNET.Coordination.Application.Queries;

public sealed record ListActivityReports(
    Guid? StudentId,
    Guid? BusinessId,
    Guid? AcademicPeriodId,
    int? Year,
    int? Month) : PagedQuery;
