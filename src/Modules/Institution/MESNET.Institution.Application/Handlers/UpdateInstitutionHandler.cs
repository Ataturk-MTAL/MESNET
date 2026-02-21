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

        institution.FullName = command.FullName;
        institution.Address = command.Address;
        institution.PhoneNumber = command.PhoneNumber;
        institution.Email = command.Email;
        institution.WebUrl = command.WebUrl;
        institution.Location = command.Location;

        session.Store(institution);

        return new InstitutionUpdated(institution.Id, institution.FullName, institution.Location);
    }
}
