using Marten;
using MESNET.Business.Application.Extensions;
using MESNET.Business.Shared.Events;
using MESNET.Common.Shared.Security;
using MESNET.Business.Core.ValueObjects;
using MESNET.Security.Shared.Events;
using Wolverine;

namespace MESNET.Business.Application.Consumers;

public static class UserCreatedConsumer
{
    public static async Task Consume(UserCreated @event, IDocumentSession session, IMessageBus bus)
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

        // İşletme Yetkilisi adı diğer modüllerin denormalize read-model'lerinde de gerekiyor
        // (Reporting → Dönem Not Fişi imza bloğu). Doğrudan şema okuması yasak, olayla taşınır (#99).
        await bus.PublishAsync(new BusinessUpdated(
            business.Id,
            business.Name,
            business.Address,
            business.Location,
            business.Sectors,
            business.PhoneNumber,
            business.Email,
            business.MasterInstructor?.FullName,
            business.PersonnelCount,
            business.PrimaryRepresentativeName(),
            // 11. argüman — resync ucuyla aynı kusur, ikinci nüsha (#295). Bu yol resync
            // çağrılmasa bile koşuyor: CompanyManager kullanıcısı oluşturulduğunda o işletmenin
            // kamu bayrağı siliniyordu.
            business.IsPublicInstitution));
    }
}
