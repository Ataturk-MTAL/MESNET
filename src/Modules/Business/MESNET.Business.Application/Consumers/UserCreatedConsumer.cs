using Marten;
using MESNET.Common.Shared.Security;
using MESNET.Business.Core.ValueObjects;
using MESNET.Security.Shared.Events;

namespace MESNET.Business.Application.Consumers;

public static class UserCreatedConsumer
{
    public static async Task Consume(UserCreated @event, IDocumentSession session)
    {
        // CompanyManager rolüyse → Business'a Representative ekle
        if (!@event.Roles.Contains(MesnetRoles.CompanyManager)) return;
        if (!@event.BusinessId.HasValue) return;

        var business = await session.LoadAsync<Core.Entities.Business>(@event.BusinessId.Value);
        if (business is null) return;

        // Aynı KeycloakId ile zaten kayıtlı mı kontrol
        if (business.Representatives.Any(r => r.KeycloakId == @event.KeycloakUserId))
            return;

        var representative = new BusinessRepresentative
        {
            KeycloakId = @event.KeycloakUserId,
            FullName = @event.FullName,
            PhoneNumber = @event.Metadata.GetValueOrDefault("Phone", ""),
            Email = @event.Email
        };

        business.Representatives.Add(representative);
        session.Store(business);
    }
}
