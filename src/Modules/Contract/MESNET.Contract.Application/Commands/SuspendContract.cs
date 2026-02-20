namespace MESNET.Contract.Application.Commands;

public sealed record SuspendContract(Guid InternshipContractId, string Reason);
