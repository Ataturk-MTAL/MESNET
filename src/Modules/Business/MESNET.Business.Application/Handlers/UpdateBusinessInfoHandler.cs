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

        if (command.Name is not null) business.Name = command.Name;
        if (command.Address is not null) business.Address = command.Address;
        if (command.PhoneNumber is not null) business.PhoneNumber = command.PhoneNumber;
        if (command.Email is not null) business.Email = command.Email;
        if (command.Website is not null) business.Website = command.Website;
        if (command.PersonnelCount is not null) business.PersonnelCount = command.PersonnelCount.Value;
        if (command.Location is not null) business.Location = command.Location;
        if (command.Sectors is not null) business.Sectors = command.Sectors;

        session.Store(business);

        return new BusinessUpdated(business.Id, business.Name, business.Address, business.Location, business.Sectors,
            business.PhoneNumber, business.Email, business.MasterInstructor?.FullName);
    }
}
