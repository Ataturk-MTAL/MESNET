using Marten;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Application.Queries;
using MESNET.Institution.Core.Entities;

namespace MESNET.Institution.Application.Handlers;

public static class GetFieldCatalogHandler
{
    public static async Task<IReadOnlyList<FieldOfStudyDto>> Handle(GetFieldCatalog query, IQuerySession session)
    {
        if (query.EducationType is not null)
        {
            var filtered = await session.Query<FieldOfStudy>()
                .Where(f => f.Type == query.EducationType && f.IsActive)
                .ToListAsync();
            return filtered.Select(f => f.ToDto()).ToList();
        }

        var all = await session.Query<FieldOfStudy>()
            .Where(f => f.IsActive)
            .ToListAsync();
        return all.Select(f => f.ToDto()).ToList();
    }
}
