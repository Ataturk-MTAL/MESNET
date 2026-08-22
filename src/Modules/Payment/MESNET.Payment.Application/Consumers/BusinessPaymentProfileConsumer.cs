using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Payment.Core.ReadModels;

namespace MESNET.Payment.Application.Consumers;

/// <summary>
/// Business modülünden gelen olaylarla Payment'ın yerel işletme profilini besler.
/// Taban ücret oranı işletmenin personel sayısına bağlı olduğu için gerekli (#64).
/// </summary>
public static class BusinessPaymentProfileConsumer
{
    public static void Consume(BusinessRegistered @event, IDocumentSession session)
    {
        session.Store(new BusinessPaymentProfile
        {
            Id = @event.BusinessId,
            Name = @event.Name,
            PersonnelCount = @event.PersonnelCount,
            IsPublicInstitution = @event.IsPublicInstitution
        });
    }

    public static async Task Consume(BusinessUpdated @event, IDocumentSession session)
    {
        // Upsert: kayıt yoksa (olay sırası veya geriye dönük veri) yeniden kur.
        var profile = await session.LoadAsync<BusinessPaymentProfile>(@event.BusinessId)
                      ?? new BusinessPaymentProfile { Id = @event.BusinessId };

        profile.Name = @event.Name;
        profile.PersonnelCount = @event.PersonnelCount;
        profile.IsPublicInstitution = @event.IsPublicInstitution;
        session.Store(profile);
    }
}
