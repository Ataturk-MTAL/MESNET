namespace MESNET.Contract.Shared.Events;

public sealed record ContractSignedByStudent(
    Guid ContractId,
    string SignedBy,
    DateTime SignedAt);
