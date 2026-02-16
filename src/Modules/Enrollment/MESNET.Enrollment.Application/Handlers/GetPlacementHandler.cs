using Marten;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Application.Queries;
using MESNET.Enrollment.Core.Entities;

namespace MESNET.Enrollment.Application.Handlers;

public static class GetPlacementHandler
{
    public static async Task<InternshipPlacementDto?> Handle(GetPlacement query, IQuerySession session)
    {
        var placement = await session.LoadAsync<InternshipPlacement>(query.PlacementId);
        return placement?.ToDto();
    }
}
