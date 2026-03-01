using MESNET.Common.Shared.Pagination;

namespace MESNET.Coordination.Application.Queries;

public sealed record ListGuidanceVisits(
    Guid? TeacherId,
    Guid? BusinessId,
    Guid? AcademicPeriodId,
    DateTime? FromDate,
    DateTime? ToDate) : PagedQuery;
