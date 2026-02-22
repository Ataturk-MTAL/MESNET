using Marten;
using MESNET.Common.Shared;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Core.Entities;
using MESNET.Institution.Core.Enums;
using MESNET.Institution.Shared.Events;
using Wolverine;

namespace MESNET.Institution.Application.Handlers;

public static class CreateAcademicPeriodHandler
{
    public static async Task<(Guid, AcademicPeriodCreated)> Handle(
        CreateAcademicPeriod command, IDocumentSession session, IMessageBus bus)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(command.InstitutionId)
            ?? throw new DomainException(InstitutionErrors.NotFound(command.InstitutionId));

        // Aynı yıl aralığında dönem var mı?
        IQueryable<AcademicPeriod> queryable = session.Query<AcademicPeriod>();
        var existing = await queryable
            .Where(p => p.InstitutionId == command.InstitutionId
                     && p.StartYear == command.StartYear
                     && p.EndYear == command.EndYear)
            .ToListAsync();

        if (existing.Count != 0)
            throw new DomainException(InstitutionErrors.AcademicPeriodAlreadyExists(command.Name));

        // Mevcut aktif dönemi kapat
        var allPeriods = await session.Query<AcademicPeriod>()
            .Where(p => p.InstitutionId == command.InstitutionId)
            .ToListAsync();

        var currentActive = allPeriods.FirstOrDefault(
            p => p.Status.Name == AcademicPeriodStatus.Active.Name);

        if (currentActive is not null)
        {
            currentActive.Status = AcademicPeriodStatus.Closed;
            currentActive.ClosedAt = DateTime.UtcNow;
            session.Store(currentActive);
            await bus.PublishAsync(new AcademicPeriodClosed(
                currentActive.Id, currentActive.InstitutionId, currentActive.ClosedAt.Value));
        }

        // Yeni dönem oluştur
        var period = new AcademicPeriod
        {
            Id = Guid.NewGuid(),
            InstitutionId = command.InstitutionId,
            Name = command.Name,
            StartYear = command.StartYear,
            EndYear = command.EndYear,
            StartDate = command.StartDate,
            EndDate = command.EndDate
        };
        session.Store(period);

        var @event = new AcademicPeriodCreated(
            period.Id, period.InstitutionId, period.Name,
            period.StartYear, period.EndYear, period.StartDate, period.EndDate);

        return (period.Id, @event);
    }
}
