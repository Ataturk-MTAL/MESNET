using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Errors;
using MESNET.Business.Shared.Events;
using MESNET.Common.Shared;

namespace MESNET.Business.Application.Handlers;

public static class UpdateBusinessInfoHandler
{
    public static async Task<BusinessUpdated> Handle(UpdateBusinessInfo command, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId);
        if (business is null)
            throw new DomainException(BusinessErrors.NotFound(command.BusinessId));

        business.Name = command.Name;
        business.Address = command.Address;
        business.PhoneNumber = command.PhoneNumber;
        business.Email = command.Email;
        business.Website = command.Website;
        business.PersonnelCount = command.PersonnelCount;
        business.Location = command.Location;

        session.Store(business);

        return new BusinessUpdated(business.Id, business.Name, business.Location);
    }
}
