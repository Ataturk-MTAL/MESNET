namespace MESNET.Contract.Shared.Events;

public sealed record ContractSignedByInstitution(
    Guid ContractId,
    string SignedBy,
    DateTime SignedAt);
