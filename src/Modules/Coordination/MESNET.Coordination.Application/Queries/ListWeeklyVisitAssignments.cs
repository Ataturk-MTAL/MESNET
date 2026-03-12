using MESNET.Common.Shared.Pagination;

namespace MESNET.Coordination.Application.Queries;

public sealed record ListWeeklyVisitAssignments(
    Guid PlanId,
    Guid InstitutionId,
    Guid? TeacherId,
    string? BranchCode) : PagedQuery;
