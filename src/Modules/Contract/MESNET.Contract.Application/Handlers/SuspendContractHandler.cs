using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Application.Errors;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class SuspendContractHandler
{
    [AggregateHandler]
    public static Result<ContractSuspended> Handle(SuspendContract command, InternshipContract contract)
    {
        if (!contract.Status.CanTransitionTo(ContractStatus.Suspended))
            return Result<ContractSuspended>.Failure(
                ContractErrors.InvalidStatus(contract.Id, contract.Status.Slug, "Sözleşme askıya alınamaz."));

        return Result<ContractSuspended>.Success(
            new ContractSuspended(contract.Id, command.Reason, DateTime.UtcNow));
    }
}
