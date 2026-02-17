using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Enrollment.Core.ReadModels;

namespace MESNET.Enrollment.Application.Consumers;

public static class BusinessCapacityChangedConsumer
{
    public static async Task Consume(BusinessCapacityChanged @event, IDocumentSession session)
    {
        var view = await session.LoadAsync<BusinessProfileView>(@event.BusinessId);
        if (view is null) return;

        view.TotalSlots = @event.TotalSlots;
        view.OccupiedSlots = @event.OccupiedSlots;
        view.LastUpdated = DateTime.UtcNow;

        session.Store(view);
    }
}
