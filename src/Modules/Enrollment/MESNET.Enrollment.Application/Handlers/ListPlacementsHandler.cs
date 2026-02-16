using Marten;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Extensions;
using MESNET.Enrollment.Application.Queries;
using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.Enums;

namespace MESNET.Enrollment.Application.Handlers;

public static class ListPlacementsHandler
{
    public static async Task<IReadOnlyList<InternshipPlacementDto>> Handle(ListPlacements query, IQuerySession session)
    {
        IQueryable<InternshipPlacement> queryable = session.Query<InternshipPlacement>();

        if (query.BusinessId.HasValue)
            queryable = queryable.Where(p => p.BusinessId == query.BusinessId.Value);

        if (query.StudentId.HasValue)
            queryable = queryable.Where(p => p.StudentId == query.StudentId.Value);

        if (!string.IsNullOrWhiteSpace(query.Status) &&
            PlacementStatus.TryFromName(query.Status, true, out var status))
            queryable = queryable.Where(p => p.Status == status);

        var placements = await queryable.ToListAsync();
        return placements.Select(p => p.ToDto()).ToList();
    }
}
