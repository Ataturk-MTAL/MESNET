using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Application.Errors;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class TerminateContractHandler
{
    [AggregateHandler]
    public static Result<ContractTerminated> Handle(TerminateContract command, InternshipContract contract)
    {
        if (!contract.Status.CanTransitionTo(ContractStatus.Terminated))
            return Result<ContractTerminated>.Failure(
                ContractErrors.InvalidStatus(contract.Id, contract.Status.Slug, "Sözleşme feshedilemez."));

        if (!TerminationReason.TryFromName(command.ReasonType, true, out _))
            return Result<ContractTerminated>.Failure(
                new Error("UNKNOWN_TERMINATION_REASON", $"Bilinmeyen fesih nedeni: {command.ReasonType}."));

        return Result<ContractTerminated>.Success(new ContractTerminated(
            contract.Id, contract.StudentId, contract.BusinessId,
            command.Reason, command.ReasonType, DateTime.UtcNow));
    }
}
