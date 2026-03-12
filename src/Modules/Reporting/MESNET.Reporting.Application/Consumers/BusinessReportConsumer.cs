using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Reporting.Core.ReadModels;

namespace MESNET.Reporting.Application.Consumers;

/// <summary>
/// Business modülü event'lerini dinleyerek StudentPlacementReportView'daki
/// işletme iletişim bilgilerini günceller (telefon, e-posta, yetkili adı).
/// </summary>
public static class BusinessReportConsumer
{
    public static async Task Consume(BusinessRegistered @event, IDocumentSession session)
    {
        await UpdateBusinessInfo(session, @event.BusinessId, @event.Name,
            @event.PhoneNumber, @event.Email, @event.MasterInstructorName);
    }

    public static async Task Consume(BusinessUpdated @event, IDocumentSession session)
    {
        await UpdateBusinessInfo(session, @event.BusinessId, @event.Name,
            @event.PhoneNumber, @event.Email, @event.MasterInstructorName);
    }

    private static async Task UpdateBusinessInfo(
        IDocumentSession session, Guid businessId, string businessName,
        string? phone, string? email, string? contactName)
    {
        // Bu işletmeye atanmış tüm öğrenci placement'larını güncelle
        var placements = await session.Query<StudentPlacementReportView>()
            .Where(v => v.BusinessId == businessId)
            .ToListAsync();

        foreach (var view in placements)
        {
            view.BusinessName = businessName;
            view.BusinessPhone = phone;
            view.BusinessEmail = email;
            view.BusinessContactName = contactName;
            session.Store(view);
        }
    }
}
