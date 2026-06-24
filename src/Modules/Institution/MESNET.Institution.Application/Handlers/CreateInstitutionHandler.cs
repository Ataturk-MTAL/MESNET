using Marten;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Shared.Events;
using Wolverine;

namespace MESNET.Institution.Application.Handlers;

public static class CreateInstitutionHandler
{
    public static async Task<Guid> Handle(CreateInstitution command, IDocumentSession session, IMessageBus bus)
    {
        var institution = new Core.Entities.Institution
        {
            Id = command.Id ?? Guid.NewGuid(),
            TenantId = command.TenantId,
            InstitutionCode = command.InstitutionCode,
            FullName = command.FullName,
            Address = command.Address,
            PhoneNumber = command.PhoneNumber,
            Email = command.Email,
            WebUrl = command.WebUrl,
            Location = command.Location
        };

        session.Store(institution);

        await bus.PublishAsync(new InstitutionUpdated(
            institution.Id, institution.FullName, institution.Location,
            institution.ScheduleConfig?.DailyPeriodCount ?? 0));

        return institution.Id;
    }
}
