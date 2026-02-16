using Marten;
using MESNET.Business.Application.ReadModels;
using MESNET.Institution.Shared.Events;

namespace MESNET.Business.Application.Consumers;

public static class BranchSpecializationsUpdatedConsumer
{
    public static async Task Consume(BranchSpecializationsUpdated @event, IDocumentSession session)
    {
        var id = GuidHelper.Combine(@event.InstitutionId, @event.FieldCode);
        var view = await session.LoadAsync<InstitutionBranchView>(id);

        if (view is not null)
        {
            view.ActiveSpecializations = @event.ActiveSpecializations;
            view.LastUpdated = DateTime.UtcNow;
            session.Store(view);
        }
    }
}
