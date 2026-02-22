using Marten;
using MESNET.Common.Shared;
using MESNET.Contract.Application.Commands;
using MESNET.Contract.Application.Errors;
using MESNET.Contract.Core.Aggregates;
using MESNET.Contract.Core.Enums;
using MESNET.Contract.Core.ReadModels;
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
        // Dönem aktiflik kontrolü
        var period = await session.LoadAsync<AcademicPeriodView>(command.AcademicPeriodId);
        if (period is null) throw new DomainException(ContractErrors.AcademicPeriodNotFound(command.AcademicPeriodId));
        if (!period.IsActive) throw new DomainException(ContractErrors.AcademicPeriodClosed(command.AcademicPeriodId));

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
            command.AcademicPeriodId,
            command.TeacherId,
            command.StartDate,
            DateTime.UtcNow);

        session.Events.StartStream<InternshipContract>(contractId, @event);
        return (contractId, @event);
    }
}
