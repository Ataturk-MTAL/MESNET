using Marten;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Application.Queries;

namespace MESNET.Institution.Application.Handlers;

public static class GetInstitutionHandler
{
    public static async Task<InstitutionDto?> Handle(GetInstitution query, IQuerySession session)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(query.InstitutionId);
        return institution?.ToDto();
    }
}
