namespace MESNET.Contract.Application.Commands;

public sealed record TerminateContract(Guid InternshipContractId, string Reason, string ReasonType, DateTime? EndDate = null) : IContractPeriodScoped;
