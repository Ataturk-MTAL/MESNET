namespace MESNET.Contract.Shared.Events;

public sealed record ContractSignedByParent(
    Guid ContractId,
    string SignedBy,
    DateTime SignedAt);
