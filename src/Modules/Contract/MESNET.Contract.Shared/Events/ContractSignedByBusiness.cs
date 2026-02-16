namespace MESNET.Contract.Shared.Events;

public sealed record ContractSignedByBusiness(
    Guid ContractId,
    string SignedBy,
    DateTime SignedAt);
