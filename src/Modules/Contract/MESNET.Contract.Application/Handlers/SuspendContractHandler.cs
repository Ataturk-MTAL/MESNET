using MESNET.Contract.Application.Commands;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class SuspendContractHandler
{
    [AggregateHandler]
    public static ContractSuspended Handle(SuspendContract command, InternshipContract contract)
    {
        if (!contract.Status.CanTransitionTo(ContractStatus.Suspended))
            throw new InvalidOperationException(
                $"Sözleşme askıya alınamaz. Mevcut durum: {contract.Status.Slug}.");

        return new ContractSuspended(contract.Id, command.Reason, DateTime.UtcNow);
    }
}
