using Marten;
using MESNET.Common.Shared;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Shared.Events;

namespace MESNET.Institution.Application.Handlers;

public static class UpdateInstitutionHandler
{
    public static async Task<InstitutionUpdated> Handle(UpdateInstitution command, IDocumentSession session)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(command.InstitutionId);
        if (institution is null)
            throw new DomainException(InstitutionErrors.NotFound(command.InstitutionId));

        if (command.FullName is not null) institution.FullName = command.FullName;
        if (command.Address is not null) institution.Address = command.Address;
        if (command.PhoneNumber is not null) institution.PhoneNumber = command.PhoneNumber;
        if (command.Email is not null) institution.Email = command.Email;
        if (command.WebUrl is not null) institution.WebUrl = command.WebUrl;
        if (command.Location is not null) institution.Location = command.Location;

        session.Store(institution);

        return new InstitutionUpdated(
            institution.Id, institution.FullName, institution.Location,
            institution.ScheduleConfig?.DailyPeriodCount ?? 0);
    }
}
