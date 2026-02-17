using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Enrollment.Core.ReadModels;

namespace MESNET.Enrollment.Application.Consumers;

public static class BusinessRegisteredConsumer
{
    public static void Consume(BusinessRegistered @event, IDocumentSession session)
    {
        var view = new BusinessProfileView
        {
            Id = @event.BusinessId,
            BusinessName = @event.Name,
            Location = @event.Location,
            LastUpdated = DateTime.UtcNow
        };

        session.Store(view);
    }
}
