namespace MESNET.Contract.Shared.Events;

public sealed record ContractCreated(
    Guid ContractId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid? TeacherId,
    DateTime StartDate,
    DateTime CreatedAt);
