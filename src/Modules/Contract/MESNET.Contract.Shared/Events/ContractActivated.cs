namespace MESNET.Contract.Shared.Events;

public sealed record ContractActivated(
    Guid ContractId,
    Guid StudentId,
    Guid BusinessId,
    DateTime ActivatedAt);
