using Marten;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Shared.Pagination;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;

namespace MESNET.Coordination.Application.Handlers;

public static class ListActivityReportsHandler
{
    public static async Task<PagedResult<MonthlyActivityReport>> Handle(
        ListActivityReports query, IQuerySession session)
    {
        IQueryable<MonthlyActivityReport> queryable = session.Query<MonthlyActivityReport>();

        if (query.StudentId.HasValue)
            queryable = queryable.Where(r => r.StudentId == query.StudentId.Value);
        if (query.BusinessId.HasValue)
            queryable = queryable.Where(r => r.BusinessId == query.BusinessId.Value);
        if (query.AcademicPeriodId.HasValue)
            queryable = queryable.Where(r => r.AcademicPeriodId == query.AcademicPeriodId.Value);
        if (query.Year.HasValue)
            queryable = queryable.Where(r => r.Year == query.Year.Value);
        if (query.Month.HasValue)
            queryable = queryable.Where(r => r.Month == query.Month.Value);

        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: r => r.Month);

        return await queryable.ToPagedResultAsync(query);
    }
}
