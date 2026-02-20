using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class SubmitContractForSignatureHandler
{
    [AggregateHandler]
    public static ContractSubmittedForSignature Handle(
        SubmitContractForSignature command, InternshipContract contract)
    {
        if (!contract.Status.CanTransitionTo(ContractStatus.AwaitingSignature))
            throw new DomainException("CONTRACT_INVALID_STATUS",
                $"Sözleşme imzaya gönderilemez. Mevcut durum: {contract.Status.Slug}.");

        return new ContractSubmittedForSignature(contract.Id, DateTime.UtcNow);
    }
}
