namespace MESNET.Contract.Shared.Events;

public sealed record ContractTerminated(
    Guid ContractId,
    Guid StudentId,
    Guid BusinessId,
    string Reason,
    string ReasonType,
    DateTime TerminatedAt);
