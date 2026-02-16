using Marten;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Shared.Events;

namespace MESNET.Institution.Application.Handlers;

public static class CreateInstitutionHandler
{
    public static InstitutionUpdated Handle(CreateInstitution command, IDocumentSession session)
    {
        var institution = new Core.Entities.Institution
        {
            Id = Guid.NewGuid(),
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

        return new InstitutionUpdated(institution.Id, institution.FullName, institution.Location);
    }
}
