using MESNET.Coordination.Core.ValueObjects;

namespace MESNET.Coordination.Application.Commands;

public sealed record CreateBusinessEvaluation(
    Guid BusinessId,
    Guid InstitutionId,
    Guid EvaluatorId,
    DateTime EvaluationDate,
    List<EvaluationItem> Items,
    string Result,
    string? Notes);

public sealed record UpdateBusinessEvaluation(
    Guid EvaluationId,
    DateTime EvaluationDate,
    List<EvaluationItem> Items,
    string Result,
    string? Notes);
