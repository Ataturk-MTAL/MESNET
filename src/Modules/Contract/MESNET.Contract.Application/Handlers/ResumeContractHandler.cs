using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Application.Errors;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class ResumeContractHandler
{
    [AggregateHandler]
    public static Result<ContractResumed> Handle(ResumeContract command, InternshipContract contract)
    {
        if (!contract.Status.CanTransitionTo(ContractStatus.Active))
            return Result<ContractResumed>.Failure(
                ContractErrors.InvalidStatus(contract.Id, contract.Status.Slug, "Sözleşme devam ettirilemez."));

        return Result<ContractResumed>.Success(
            new ContractResumed(contract.Id, DateTime.UtcNow));
    }
}
