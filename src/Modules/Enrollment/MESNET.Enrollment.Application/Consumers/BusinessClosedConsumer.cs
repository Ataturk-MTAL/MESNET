using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Enrollment.Application.ReadModels;

namespace MESNET.Enrollment.Application.Consumers;

public static class BusinessClosedConsumer
{
    public static async Task Consume(BusinessClosed @event, IDocumentSession session)
    {
        var view = await session.LoadAsync<BusinessProfileView>(@event.BusinessId);
        if (view is null) return;

        view.IsActive = false;
        view.LastUpdated = DateTime.UtcNow;

        session.Store(view);
    }
}
