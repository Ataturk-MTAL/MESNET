using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;

namespace MESNET.Coordination.Application.Handlers;

public static class GetGuidanceVisitHandler
{
    public static async Task<GuidanceVisit> Handle(
        GetGuidanceVisit query, IQuerySession session)
    {
        var visit = await session.LoadAsync<GuidanceVisit>(query.VisitId);
        if (visit is null)
            throw new DomainException(new Error("GuidanceVisit.NotFound", $"Rehberlik ziyareti bulunamadı: {query.VisitId}"));

        return visit;
    }
}
