using Marten;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Shared.Pagination;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;

namespace MESNET.Coordination.Application.Handlers;

public static class ListGuidanceVisitsHandler
{
    public static async Task<PagedResult<GuidanceVisit>> Handle(
        ListGuidanceVisits query, IQuerySession session)
    {
        IQueryable<GuidanceVisit> queryable = session.Query<GuidanceVisit>();

        if (query.TeacherId.HasValue)
            queryable = queryable.Where(v => v.TeacherId == query.TeacherId.Value);
        if (query.BusinessId.HasValue)
            queryable = queryable.Where(v => v.BusinessId == query.BusinessId.Value);
        if (query.AcademicPeriodId.HasValue)
            queryable = queryable.Where(v => v.AcademicPeriodId == query.AcademicPeriodId.Value);
        if (query.FromDate.HasValue)
            queryable = queryable.Where(v => v.VisitDate >= query.FromDate.Value);
        if (query.ToDate.HasValue)
            queryable = queryable.Where(v => v.VisitDate <= query.ToDate.Value);

        queryable = queryable.ApplySearch(query.Search, v => v.InstructorMeetingNotes);
        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: v => v.VisitDate);

        return await queryable.ToPagedResultAsync(query);
    }
}
