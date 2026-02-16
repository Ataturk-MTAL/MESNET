using Marten;
using MESNET.Business.Application.Dtos;
using MESNET.Business.Application.Extensions;
using MESNET.Business.Application.Queries;

namespace MESNET.Business.Application.Handlers;

public static class GetBusinessesByStatusHandler
{
    public static async Task<IReadOnlyList<BusinessDto>> Handle(
        GetBusinessesByStatus query, IQuerySession session)
    {
        var businesses = await session.Query<Core.Entities.Business>()
            .Where(b => b.Status == query.Status)
            .ToListAsync();

        return businesses.Select(b => b.ToDto()).ToList();
    }
}
