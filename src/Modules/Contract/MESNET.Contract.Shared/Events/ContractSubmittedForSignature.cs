namespace MESNET.Contract.Shared.Events;

public sealed record ContractSubmittedForSignature(
    Guid ContractId,
    DateTime SubmittedAt);
