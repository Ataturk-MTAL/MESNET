using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Application.Errors;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class CompleteContractHandler
{
    [AggregateHandler]
    public static Result<ContractCompleted> Handle(CompleteContract command, InternshipContract contract)
    {
        if (!contract.Status.CanTransitionTo(ContractStatus.Completed))
            return Result<ContractCompleted>.Failure(
                ContractErrors.InvalidStatus(contract.Id, contract.Status.Slug, "Sözleşme tamamlanamaz."));

        return Result<ContractCompleted>.Success(new ContractCompleted(
            contract.Id, contract.StudentId, contract.BusinessId, DateTime.UtcNow));
    }
}
