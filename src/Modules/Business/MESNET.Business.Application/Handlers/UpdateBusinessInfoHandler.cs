using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Shared.Events;

namespace MESNET.Business.Application.Handlers;

public static class UpdateBusinessInfoHandler
{
    public static async Task<BusinessUpdated> Handle(UpdateBusinessInfo command, IDocumentSession session)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(command.BusinessId)
            ?? throw new InvalidOperationException($"İşletme bulunamadı: {command.BusinessId}");

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
