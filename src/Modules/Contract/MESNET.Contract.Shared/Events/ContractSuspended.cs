namespace MESNET.Contract.Shared.Events;

public sealed record ContractSuspended(
    Guid ContractId,
    string Reason,
    DateTime SuspendedAt);
