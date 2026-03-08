using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Coordination.Application.Helpers;
using MESNET.Coordination.Application.Services;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// İşletme bilgileri güncellendiğinde coordination view'ı günceller (isim, adres, lokasyon).
/// Lokasyon değiştiyse otomatik mesafe yeniden hesaplar (OSRM → Haversine fallback).
/// </summary>
public static class BusinessUpdatedCoordinationConsumer
{
    public static async Task Consume(
        BusinessUpdated @event,
        IDocumentSession session,
        IOsrmDistanceService osrmService,
        CancellationToken cancellationToken)
    {
        var view = await session.LoadAsync<BusinessCoordinationView>(@event.BusinessId, cancellationToken);
        if (view is null) return;

        var locationChanged = @event.Location != view.Location;

        view.Name = @event.Name;
        view.Address = @event.Address;
        view.District = AddressHelper.ExtractDistrict(@event.Address);
        view.Location = @event.Location;

        // Lokasyon değiştiyse ve manuel mesafe girilmemişse yeniden hesapla
        if (locationChanged && !view.IsManualDistance && @event.Location is not null)
        {
            await DistanceHelper.CalculateAndSetDistanceAsync(
                view, view.InstitutionId, session, osrmService, cancellationToken);
        }

        session.Store(view);
    }
}
