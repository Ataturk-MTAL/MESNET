using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Coordination.Application.Helpers;
using MESNET.Coordination.Application.Services;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// İşletme bilgileri güncellendiğinde coordination view'ı günceller (isim, adres, lokasyon).
/// Lokasyon değiştiyse otomatik mesafe yeniden hesaplar (OSRM → Haversine fallback).
///
/// İşletme düzeyi bir olaydır: işletmenin <b>tüm alan satırları</b> + temel satırı güncellenir.
/// Mesafe bir kez hesaplanıp tüm satırlara kopyalanır (tek OSRM çağrısı).
/// </summary>
public static class BusinessUpdatedCoordinationConsumer
{
    public static async Task Consume(
        BusinessUpdated @event,
        IDocumentSession session,
        IOsrmDistanceService osrmService,
        CancellationToken cancellationToken)
    {
        var rows = await session.Query<BusinessCoordinationView>()
            .Where(v => v.BusinessId == @event.BusinessId || v.Id == @event.BusinessId)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0) return;

        // Mesafe referansı: temel satır varsa o, yoksa ilk satır — tek kez hesaplanır.
        var reference = rows.FirstOrDefault(r => string.IsNullOrWhiteSpace(r.BranchCode)) ?? rows[0];
        var locationChanged = @event.Location != reference.Location;

        foreach (var row in rows)
        {
            row.Name = @event.Name;
            row.Address = @event.Address;
            row.District = AddressHelper.ExtractDistrict(@event.Address);
            row.Location = @event.Location;
        }

        // Lokasyon değiştiyse ve manuel mesafe girilmemişse yeniden hesapla
        if (locationChanged && !reference.IsManualDistance && @event.Location is not null)
        {
            await DistanceHelper.CalculateAndSetDistanceAsync(
                reference, reference.InstitutionId, session, osrmService, cancellationToken);

            foreach (var row in rows)
                DistanceHelper.CopyDistanceTo(reference, row);
        }

        foreach (var row in rows)
            session.Store(row);
    }
}
