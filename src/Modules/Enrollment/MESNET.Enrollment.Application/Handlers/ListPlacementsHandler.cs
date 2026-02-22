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

        if (query.AcademicPeriodId.HasValue)
            queryable = queryable.Where(p => p.AcademicPeriodId == query.AcademicPeriodId.Value);

        var placements = await queryable.ToListAsync();

        // SmartEnum LINQ kısıtı: in-memory filtrele
        if (!string.IsNullOrWhiteSpace(query.Status) &&
            PlacementStatus.TryFromName(query.Status, true, out var status))
            placements = placements.Where(p => p.Status.Name == status.Name).ToList();

        return placements.Select(p => p.ToDto()).ToList();
    }
}
