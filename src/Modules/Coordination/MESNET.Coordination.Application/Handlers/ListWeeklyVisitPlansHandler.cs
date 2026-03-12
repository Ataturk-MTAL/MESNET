using Marten;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Shared.Pagination;
using MESNET.Coordination.Application.Dtos;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;

namespace MESNET.Coordination.Application.Handlers;

public static class ListWeeklyVisitPlansHandler
{
    public static async Task<PagedResult<WeeklyVisitPlanDto>> Handle(
        ListWeeklyVisitPlans query,
        IQuerySession session)
    {
        IQueryable<WeeklyVisitPlan> q = session.Query<WeeklyVisitPlan>()
            .Where(p => p.InstitutionId == query.InstitutionId);

        if (query.AcademicPeriodId.HasValue)
            q = q.Where(p => p.AcademicPeriodId == query.AcademicPeriodId.Value);

        if (query.Year.HasValue)
            q = q.Where(p => p.Year == query.Year.Value);

        if (query.WeekNumber.HasValue)
            q = q.Where(p => p.WeekNumber == query.WeekNumber.Value);

        q = q.ApplySort(query.SortBy, query.Descending, defaultSort: p => p.WeekNumber);

        return await q.ToPagedResultAsync(query, p => new WeeklyVisitPlanDto(
            p.Id,
            p.AcademicPeriodId,
            p.Year,
            p.WeekNumber,
            p.WeekStartDate.ToString("yyyy-MM-dd"),
            p.WeekEndDate.ToString("yyyy-MM-dd"),
            p.Scope,
            p.ScopeTeacherId,
            p.ScopeBranchCode,
            p.AssignmentCount,
            p.GeneratedBy,
            p.GeneratedAt));
    }
}
