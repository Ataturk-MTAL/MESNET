using Marten;
using MESNET.Enrollment.Core.ReadModels;
using MESNET.Institution.Shared.Events;

namespace MESNET.Enrollment.Application.Consumers;

public static class AcademicPeriodClosedConsumer
{
    public static async Task Consume(AcademicPeriodClosed @event, IDocumentSession session)
    {
        var view = await session.LoadAsync<AcademicPeriodView>(@event.AcademicPeriodId);
        if (view is null) return;

        view.IsActive = false;
        view.ClosedAt = @event.ClosedAt;
        session.Store(view);
    }
}
