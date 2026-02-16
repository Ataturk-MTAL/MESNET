namespace MESNET.Contract.Shared.Events;

public sealed record ContractResumed(
    Guid ContractId,
    DateTime ResumedAt);
