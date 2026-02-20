using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Application.Handlers;

public static class BusinessEvaluationHandler
{
    public static async Task<Guid> Handle(
        CreateBusinessEvaluation command,
        IDocumentSession session,
        CancellationToken ct)
    {
        if (!EvaluationResult.TryFromName(command.Result, true, out var evalResult))
            throw new DomainException(CoordinationErrors.InvalidEvaluationResult(command.Result));

        var evaluation = new BusinessEvaluation
        {
            Id = Guid.NewGuid(),
            BusinessId = command.BusinessId,
            InstitutionId = command.InstitutionId,
            EvaluatorId = command.EvaluatorId,
            EvaluationDate = command.EvaluationDate,
            Items = command.Items,
            Result = evalResult,
            Notes = command.Notes
        };

        session.Store(evaluation);
        await session.SaveChangesAsync(ct);

        return evaluation.Id;
    }

    public static async Task Handle(
        UpdateBusinessEvaluation command,
        IDocumentSession session,
        CancellationToken ct)
    {
        var evaluation = await session.LoadAsync<BusinessEvaluation>(command.EvaluationId, ct);
        if (evaluation is null)
            throw new DomainException(CoordinationErrors.EvaluationNotFound(command.EvaluationId));

        if (!EvaluationResult.TryFromName(command.Result, true, out var evalResult))
            throw new DomainException(CoordinationErrors.InvalidEvaluationResult(command.Result));

        evaluation.EvaluationDate = command.EvaluationDate;
        evaluation.Items = command.Items;
        evaluation.Result = evalResult;
        evaluation.Notes = command.Notes;

        session.Store(evaluation);
        await session.SaveChangesAsync(ct);
    }
}
