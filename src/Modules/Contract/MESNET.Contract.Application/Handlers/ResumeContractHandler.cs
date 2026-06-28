using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class ResumeContractHandler
{
    [AggregateHandler]
    public static ContractResumed Handle(ResumeContract command, InternshipContract? contract)
    {
        if (contract is null)
            throw new DomainException("CONTRACT_NOT_FOUND", "Sözleşme bulunamadı.");

        if (!contract.Status.CanTransitionTo(ContractStatus.Active))
            throw new DomainException("CONTRACT_INVALID_STATUS",
                $"Sözleşme devam ettirilemez. Mevcut durum: {contract.Status.Slug}.");

        return new ContractResumed(contract.Id, DateTime.UtcNow);
    }
}
