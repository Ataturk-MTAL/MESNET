using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Reporting.Core.ReadModels;

namespace MESNET.Reporting.Application.Consumers;

/// <summary>
/// Business modülü event'lerini dinleyerek Reporting'in yerel işletme verisini günceller:
/// (1) <see cref="BusinessContactReportView"/> — işletmeye özel iletişim/yetkili kaydı,
/// (2) <see cref="StudentPlacementReportView"/> — mevcut yerleştirmelerdeki denormalize kopyalar.
/// (1) yerleştirme sırasına bağlı değildir; işletme olayı yerleştirmeden önce gelse de veri kaybolmaz.
/// </summary>
public static class BusinessReportConsumer
{
    public static async Task Consume(BusinessRegistered @event, IDocumentSession session)
    {
        await UpdateBusinessInfo(session, @event.BusinessId, @event.Name,
            @event.PhoneNumber, @event.Email, @event.MasterInstructorName, @event.RepresentativeName);
    }

    public static async Task Consume(BusinessUpdated @event, IDocumentSession session)
    {
        await UpdateBusinessInfo(session, @event.BusinessId, @event.Name,
            @event.PhoneNumber, @event.Email, @event.MasterInstructorName, @event.RepresentativeName);
    }

    private static async Task UpdateBusinessInfo(
        IDocumentSession session, Guid businessId, string businessName,
        string? phone, string? email, string? masterInstructorName, string? representativeName)
    {
        await UpsertContactView(session, businessId, businessName, phone, email,
            masterInstructorName, representativeName);

        // Bu işletmeye atanmış tüm öğrenci placement'larını güncelle
        var placements = await session.Query<StudentPlacementReportView>()
            .Where(v => v.BusinessId == businessId)
            .ToListAsync();

        foreach (var view in placements)
        {
            view.BusinessName = businessName;
            view.BusinessPhone = phone;
            view.BusinessEmail = email;
            view.BusinessContactName = masterInstructorName;
            session.Store(view);
        }
    }

    private static async Task UpsertContactView(
        IDocumentSession session, Guid businessId, string businessName,
        string? phone, string? email, string? masterInstructorName, string? representativeName)
    {
        var contact = await session.LoadAsync<BusinessContactReportView>(businessId)
                      ?? new BusinessContactReportView { Id = businessId };

        contact.BusinessName = businessName;
        contact.PhoneNumber = phone;
        contact.Email = email;
        contact.MasterInstructorName = masterInstructorName;
        // Yetkili yalnız UserCreated/self-register akışlarında dolar; boş gelen olay mevcut adı silmesin.
        contact.RepresentativeName = representativeName ?? contact.RepresentativeName;
        contact.UpdatedAt = DateTime.UtcNow;

        session.Store(contact);
    }
}
