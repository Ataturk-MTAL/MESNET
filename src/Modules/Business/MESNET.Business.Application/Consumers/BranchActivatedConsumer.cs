using Marten;
using MESNET.Business.Core.ReadModels;
using MESNET.Institution.Shared.Events;

namespace MESNET.Business.Application.Consumers;

public static class BranchActivatedConsumer
{
    public static void Consume(BranchActivated @event, IDocumentSession session)
    {
        var view = new InstitutionBranchView
        {
            Id = GuidHelper.Combine(@event.InstitutionId, @event.FieldCode),
            InstitutionId = @event.InstitutionId,
            FieldCode = @event.FieldCode,
            FieldName = @event.FieldName,
            EducationType = @event.Type.Name,
            IsActive = true,
            LastUpdated = DateTime.UtcNow
        };

        session.Store(view);
    }
}
