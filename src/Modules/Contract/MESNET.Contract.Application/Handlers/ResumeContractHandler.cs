using MESNET.Contract.Application.Commands;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class ResumeContractHandler
{
    [AggregateHandler]
    public static ContractResumed Handle(ResumeContract command, InternshipContract contract)
    {
        if (!contract.Status.CanTransitionTo(ContractStatus.Active))
            throw new InvalidOperationException(
                $"Sözleşme devam ettirilemez. Mevcut durum: {contract.Status.Slug}.");

        return new ContractResumed(contract.Id, DateTime.UtcNow);
    }
}
