namespace MESNET.Coordination.Core.ValueObjects;

public sealed record EvaluationItem(
    string Category,
    string Question,
    bool IsCompliant,
    string? Note);
