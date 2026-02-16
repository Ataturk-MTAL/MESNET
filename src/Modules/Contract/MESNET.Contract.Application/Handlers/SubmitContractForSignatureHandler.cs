using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Application.Errors;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class SubmitContractForSignatureHandler
{
    [AggregateHandler]
    public static Result<ContractSubmittedForSignature> Handle(
        SubmitContractForSignature command, InternshipContract contract)
    {
        if (!contract.Status.CanTransitionTo(ContractStatus.AwaitingSignature))
            return Result<ContractSubmittedForSignature>.Failure(
                ContractErrors.InvalidStatus(contract.Id, contract.Status.Slug, "Sözleşme imzaya gönderilemez."));

        return Result<ContractSubmittedForSignature>.Success(
            new ContractSubmittedForSignature(contract.Id, DateTime.UtcNow));
    }
}
