using Marten;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Shared.Pagination;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Application.Queries;
using MESNET.Institution.Core.Entities;

namespace MESNET.Institution.Application.Handlers;

public static class ListAcademicPeriodsHandler
{
    public static async Task<PagedResult<AcademicPeriodDto>> Handle(
        ListAcademicPeriods query, IQuerySession session)
    {
        IQueryable<AcademicPeriod> queryable = session.Query<AcademicPeriod>()
            .Where(p => p.InstitutionId == query.InstitutionId);

        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: p => p.StartDate);

        return await queryable.ToPagedResultAsync(query, p => p.ToDto());
    }
}
