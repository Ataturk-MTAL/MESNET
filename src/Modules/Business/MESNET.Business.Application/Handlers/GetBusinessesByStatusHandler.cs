using Marten;
using MESNET.Business.Application.Dtos;
using MESNET.Business.Application.Extensions;
using MESNET.Business.Application.Queries;
using MESNET.Business.Core.Enums;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Shared.Pagination;

namespace MESNET.Business.Application.Handlers;

public static class GetBusinessesByStatusHandler
{
    public static async Task<PagedResult<BusinessDto>> Handle(
        GetBusinessesByStatus query, IQuerySession session)
    {
        IQueryable<Core.Entities.Business> queryable = session.Query<Core.Entities.Business>();

        if (query.Status is not null && BusinessStatus.TryFromName(query.Status, true, out var businessStatus))
            queryable = queryable.Where(b => b.StatusName == businessStatus.Name);

        if (query.Sector is not null && BusinessSector.TryFromName(query.Sector, true, out var sector))
            queryable = queryable.Where(b => b.Sectors.Any(s => s == sector.Name));

        queryable = queryable.ApplySearch(query.Search, b => b.Name, b => b.Address);
        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: b => b.Name);

        return await queryable.ToPagedResultAsync(query, b => b.ToDto());
    }
}
