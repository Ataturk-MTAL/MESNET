using MESNET.Common.Shared.Pagination;

namespace MESNET.Coordination.Application.Queries;

public sealed record ListWeeklyVisitPlans(
    Guid InstitutionId,
    Guid? AcademicPeriodId,
    int? Year,
    int? WeekNumber) : PagedQuery;
