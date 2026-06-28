namespace MESNET.Contract.Application.Commands;

public sealed record SignContract(Guid InternshipContractId, string Party, string SignedBy) : IContractPeriodScoped;
