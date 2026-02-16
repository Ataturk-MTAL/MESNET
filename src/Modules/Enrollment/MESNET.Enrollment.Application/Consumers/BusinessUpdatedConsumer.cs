using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Enrollment.Application.ReadModels;

namespace MESNET.Enrollment.Application.Consumers;

public static class BusinessUpdatedConsumer
{
    public static async Task Consume(BusinessUpdated @event, IDocumentSession session)
    {
        var view = await session.LoadAsync<BusinessProfileView>(@event.BusinessId);
        if (view is null) return;

        view.BusinessName = @event.Name;
        view.Location = @event.Location;
        view.LastUpdated = DateTime.UtcNow;

        session.Store(view);
    }
}
