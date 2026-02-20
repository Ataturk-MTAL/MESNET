using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class ActivateContractHandler
{
    [AggregateHandler]
    public static ContractActivated Handle(ActivateContract command, InternshipContract contract)
    {
        if (!contract.Status.CanTransitionTo(ContractStatus.Active))
            throw new DomainException("CONTRACT_INVALID_STATUS",
                $"Sözleşme aktif edilemez. Mevcut durum: {contract.Status.Slug}.");

        if (!contract.AllSignaturesComplete)
            throw new DomainException("CONTRACT_SIGNATURES_INCOMPLETE", "Tüm tarafların imzası tamamlanmalı.");

        return new ContractActivated(contract.Id, contract.StudentId, contract.BusinessId, DateTime.UtcNow);
    }
}
