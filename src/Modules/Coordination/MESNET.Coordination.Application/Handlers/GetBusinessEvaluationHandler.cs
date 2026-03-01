using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;

namespace MESNET.Coordination.Application.Handlers;

public static class GetBusinessEvaluationHandler
{
    public static async Task<BusinessEvaluation> Handle(
        GetBusinessEvaluation query, IQuerySession session)
    {
        var evaluation = await session.LoadAsync<BusinessEvaluation>(query.EvaluationId);
        if (evaluation is null)
            throw new DomainException(new Error("BusinessEvaluation.NotFound", $"İşletme değerlendirmesi bulunamadı: {query.EvaluationId}"));

        return evaluation;
    }
}
