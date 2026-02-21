using Marten;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Application.Queries;
using MESNET.Institution.Core.Entities;

namespace MESNET.Institution.Application.Handlers;

public static class GetFieldCatalogHandler
{
    public static async Task<List<FieldOfStudyDto>> Handle(GetFieldCatalog query, IQuerySession session)
    {
        // Marten SmartEnum LINQ kısıtı: SmartEnum doğrudan karşılaştırılamaz.
        // Tüm aktif kayıtları çekip in-memory filtrele.
        var all = await session.Query<FieldOfStudy>()
            .Where(f => f.IsActive)
            .ToListAsync();

        if (query.EducationType is not null)
        {
            var filtered = all.Where(f => f.Type.Name == query.EducationType.Name).ToList();
            return filtered.Select(f => f.ToDto()).ToList();
        }

        return all.Select(f => f.ToDto()).ToList();
    }
}
