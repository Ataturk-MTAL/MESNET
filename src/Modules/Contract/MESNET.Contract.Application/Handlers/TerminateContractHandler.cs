using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class TerminateContractHandler
{
    [AggregateHandler]
    public static ContractTerminated Handle(TerminateContract command, InternshipContract contract)
    {
        if (!contract.Status.CanTransitionTo(ContractStatus.Terminated))
            throw new DomainException("CONTRACT_INVALID_STATUS",
                $"Sözleşme feshedilemez. Mevcut durum: {contract.Status.Slug}.");

        if (!TerminationReason.TryFromName(command.ReasonType, true, out _))
            throw new DomainException("CONTRACT_UNKNOWN_TERMINATION_REASON",
                $"Bilinmeyen fesih nedeni: {command.ReasonType}.");

        var endDate = command.EndDate ?? DateTime.UtcNow;
        return new ContractTerminated(
            contract.Id, contract.StudentId, contract.BusinessId,
            command.Reason, command.ReasonType, endDate, DateTime.UtcNow);
    }
}
