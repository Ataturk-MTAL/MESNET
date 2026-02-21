using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class CompleteContractHandler
{
    [AggregateHandler]
    public static ContractCompleted Handle(CompleteContract command, InternshipContract contract)
    {
        if (!contract.Status.CanTransitionTo(ContractStatus.Completed))
            throw new DomainException("CONTRACT_INVALID_STATUS",
                $"Sözleşme tamamlanamaz. Mevcut durum: {contract.Status.Slug}.");

        var endDate = command.EndDate ?? DateTime.UtcNow;
        return new ContractCompleted(contract.Id, contract.StudentId, contract.BusinessId, endDate, DateTime.UtcNow);
    }
}
