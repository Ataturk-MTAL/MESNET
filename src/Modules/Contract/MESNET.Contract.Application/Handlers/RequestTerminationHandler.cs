using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class RequestTerminationHandler
{
    [AggregateHandler]
    public static ContractTerminationRequested Handle(RequestTermination command, InternshipContract? contract)
    {
        if (contract is null)
            throw new DomainException("CONTRACT_NOT_FOUND", "Sözleşme bulunamadı.");

        if (!contract.Status.CanTransitionTo(ContractStatus.TerminationRequested))
            throw new DomainException("CONTRACT_INVALID_STATUS",
                $"Bu sözleşme için fesih talebi oluşturulamaz. Mevcut durum: {contract.Status.Slug}.");

        if (!TerminationReason.TryFromName(command.ReasonType, true, out _))
            throw new DomainException("CONTRACT_UNKNOWN_TERMINATION_REASON",
                $"Bilinmeyen fesih nedeni: {command.ReasonType}.");

        return new ContractTerminationRequested(
            contract.Id,
            contract.StudentId,
            contract.BusinessId,
            command.Reason,
            command.ReasonType,
            command.RequestedBy,
            DateTime.UtcNow);
    }
}
