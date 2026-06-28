using Marten;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;

namespace MESNET.Coordination.Application.Handlers;

public static class GetCoordinationConfigHandler
{
    public static async Task<CoordinationConfig> Handle(GetCoordinationConfig query, IQuerySession session)
    {
        var config = await session.LoadAsync<CoordinationConfig>(query.InstitutionId);
        return config ?? new CoordinationConfig { Id = query.InstitutionId, InstitutionId = query.InstitutionId };
    }
}
