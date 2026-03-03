using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Coordination.Application.Helpers;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// İşletme bilgileri güncellendiğinde coordination view'ı günceller (isim, adres, lokasyon).
/// </summary>
public static class BusinessUpdatedCoordinationConsumer
{
    public static async Task Consume(
        BusinessUpdated @event,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var view = await session.LoadAsync<BusinessCoordinationView>(@event.BusinessId, cancellationToken);
        if (view is null) return;

        view.Name = @event.Name;
        view.Address = @event.Address;
        view.District = AddressHelper.ExtractDistrict(@event.Address);
        view.Location = @event.Location;

        session.Store(view);
    }
}
