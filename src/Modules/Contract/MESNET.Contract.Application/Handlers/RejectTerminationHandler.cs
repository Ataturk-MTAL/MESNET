using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Contract.Application.Handlers;

public static class RejectTerminationHandler
{
    [AggregateHandler]
    public static ContractTerminationRejected Handle(RejectTermination command, InternshipContract contract)
    {
        if (contract.Status != ContractStatus.TerminationRequested)
            throw new DomainException("CONTRACT_INVALID_STATUS",
                $"Reddedilebilecek fesih talebi yok. Mevcut durum: {contract.Status.Slug}.");

        return new ContractTerminationRejected(
            contract.Id,
            contract.StudentId,
            contract.BusinessId,
            command.RejectedBy,
            command.RejectionNote,
            DateTime.UtcNow);
    }
}
