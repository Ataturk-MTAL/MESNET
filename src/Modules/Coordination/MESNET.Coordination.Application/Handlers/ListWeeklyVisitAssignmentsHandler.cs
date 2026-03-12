using Marten;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Shared.Pagination;
using MESNET.Coordination.Application.Dtos;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;

namespace MESNET.Coordination.Application.Handlers;

public static class ListWeeklyVisitAssignmentsHandler
{
    public static async Task<PagedResult<WeeklyVisitAssignmentDto>> Handle(
        ListWeeklyVisitAssignments query,
        IQuerySession session)
    {
        IQueryable<WeeklyVisitAssignment> q = session.Query<WeeklyVisitAssignment>()
            .Where(a => a.PlanId == query.PlanId && a.InstitutionId == query.InstitutionId);

        if (query.TeacherId.HasValue)
            q = q.Where(a => a.TeacherId == query.TeacherId.Value);

        if (!string.IsNullOrWhiteSpace(query.BranchCode))
            q = q.Where(a => a.BranchCode == query.BranchCode);

        q = q.ApplySearch(query.Search, a => a.TeacherName, a => a.BusinessName);
        q = q.ApplySort(query.SortBy, query.Descending, defaultSort: a => a.VisitDate);

        return await q.ToPagedResultAsync(query, a => new WeeklyVisitAssignmentDto(
            a.Id,
            a.PlanId,
            a.TeacherId,
            a.TeacherName,
            a.BusinessId,
            a.BusinessName,
            a.BranchCode,
            a.BranchName,
            a.VisitDate.ToString("yyyy-MM-dd"),
            a.Day,
            a.PeriodCount,
            a.WeekNumber));
    }
}
