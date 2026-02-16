namespace MESNET.Contract.Application.Commands;

public sealed record TerminateContract(Guid ContractId, string Reason, string ReasonType);
