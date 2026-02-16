using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Application.Errors;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class ActivateContractHandler
{
    [AggregateHandler]
    public static Result<ContractActivated> Handle(ActivateContract command, InternshipContract contract)
    {
        if (!contract.Status.CanTransitionTo(ContractStatus.Active))
            return Result<ContractActivated>.Failure(
                ContractErrors.InvalidStatus(contract.Id, contract.Status.Slug, "Sözleşme aktif edilemez."));

        if (!contract.AllSignaturesComplete)
            return Result<ContractActivated>.Failure(
                new Error("SIGNATURES_INCOMPLETE", "Tüm tarafların imzası tamamlanmalı."));

        return Result<ContractActivated>.Success(new ContractActivated(
            contract.Id, contract.StudentId, contract.BusinessId, DateTime.UtcNow));
    }
}
