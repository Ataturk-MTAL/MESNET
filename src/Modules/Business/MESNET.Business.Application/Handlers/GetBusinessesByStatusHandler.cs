using Marten;
using MESNET.Business.Application.Dtos;
using MESNET.Business.Application.Extensions;
using MESNET.Business.Application.Queries;
using MESNET.Business.Core.Enums;

namespace MESNET.Business.Application.Handlers;

public static class GetBusinessesByStatusHandler
{
    public static async Task<IReadOnlyList<BusinessDto>> Handle(
        GetBusinessesByStatus query, IQuerySession session)
    {
        IQueryable<Core.Entities.Business> queryable = session.Query<Core.Entities.Business>();

        if (query.Status is not null && BusinessStatus.TryFromName(query.Status, true, out var businessStatus))
            queryable = queryable.Where(b => b.Status.Name == businessStatus.Name);

        if (query.Sector is not null && BusinessSector.TryFromName(query.Sector, true, out var sector))
            queryable = queryable.Where(b => b.Sectors.Any(s => s == sector.Name));

        var businesses = await queryable.ToListAsync();
        return businesses.Select(b => b.ToDto()).ToList();
    }
}
