namespace MESNET.Contract.Application.Commands;

public sealed record CompleteContract(Guid InternshipContractId, DateTime? EndDate = null);
