namespace MESNET.Contract.Application.Commands;

public sealed record SignContract(Guid ContractId, string Party, string SignedBy);
