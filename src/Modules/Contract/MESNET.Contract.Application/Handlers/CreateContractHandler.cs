using Marten;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Shared.Events;

namespace MESNET.Contract.Application.Handlers;

public static class CreateContractHandler
{
    public static (Guid, ContractCreated) Handle(CreateContract command, IDocumentSession session)
    {
        var contractId = Guid.NewGuid();
        var @event = new ContractCreated(
            contractId,
            command.StudentId,
            command.BusinessId,
            command.InstitutionId,
            command.TeacherId,
            command.StartDate,
            DateTime.UtcNow);

        session.Events.StartStream<InternshipContract>(contractId, @event);
        return (contractId, @event);
    }
}
