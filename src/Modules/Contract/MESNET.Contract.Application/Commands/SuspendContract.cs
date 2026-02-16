namespace MESNET.Contract.Application.Commands;

public sealed record SuspendContract(Guid ContractId, string Reason);
