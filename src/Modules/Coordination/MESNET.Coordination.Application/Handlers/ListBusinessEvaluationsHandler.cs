using Marten;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Shared.Pagination;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;

namespace MESNET.Coordination.Application.Handlers;

public static class ListBusinessEvaluationsHandler
{
    public static async Task<PagedResult<BusinessEvaluation>> Handle(
        ListBusinessEvaluations query, IQuerySession session)
    {
        IQueryable<BusinessEvaluation> queryable = session.Query<BusinessEvaluation>();

        if (query.BusinessId.HasValue)
            queryable = queryable.Where(e => e.BusinessId == query.BusinessId.Value);
        if (query.InstitutionId.HasValue)
            queryable = queryable.Where(e => e.InstitutionId == query.InstitutionId.Value);

        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: e => e.EvaluationDate);

        return await queryable.ToPagedResultAsync(query);
    }
}
