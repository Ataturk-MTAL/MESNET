using Marten;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Application.Queries;

namespace MESNET.Institution.Application.Handlers;

public static class GetInstitutionsHandler
{
    public static async Task<List<InstitutionDto>> Handle(GetInstitutions query, IQuerySession session)
    {
        var institutions = await session.Query<Core.Entities.Institution>().ToListAsync();
        return institutions.Select(i => i.ToDto()).ToList();
    }
}
