namespace MESNET.Contract.Shared.Events;

public sealed record ContractCompleted(
    Guid ContractId,
    Guid StudentId,
    Guid BusinessId,
    DateTime CompletedAt);
