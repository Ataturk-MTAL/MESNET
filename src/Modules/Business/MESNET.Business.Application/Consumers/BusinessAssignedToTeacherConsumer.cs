using Marten;
using MESNET.Coordination.Shared.Events;

namespace MESNET.Business.Application.Consumers;

public static class BusinessAssignedToTeacherConsumer
{
    public static async Task Consume(
        BusinessAssignedToTeacher @event,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(@event.BusinessId, cancellationToken);
        if (business is null) return;

        business.HasAssignedTeacher = true;
        session.Store(business);
    }
}
