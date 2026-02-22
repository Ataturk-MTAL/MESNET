using Marten;
using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Application.Errors;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Shared.Events;

namespace MESNET.Contract.Application.Handlers;

public static class CreateContractHandler
{
    private static readonly string[] ActiveStatuses =
    [
        ContractStatus.Draft.Name,
        ContractStatus.AwaitingSignature.Name,
        ContractStatus.Active.Name,
        ContractStatus.Suspended.Name,
    ];

    public static async Task<(Guid, ContractCreated)> Handle(CreateContract command, IDocumentSession session)
    {
        // Öğrencinin devam eden sözleşmesi var mı kontrol et
        IQueryable<InternshipContract> queryable = session.Query<InternshipContract>();
        var existing = await queryable
            .Where(c => c.StudentId == command.StudentId)
            .ToListAsync();

        var hasActive = existing.Any(c => ActiveStatuses.Contains(c.Status.Name));
        if (hasActive)
            throw new DomainException(ContractErrors.ActiveContractExists(command.StudentId));

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
