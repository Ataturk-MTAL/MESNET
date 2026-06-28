using Marten;
using MESNET.Institution.Shared.Events;
using MESNET.Payment.Core.ReadModels;

namespace MESNET.Payment.Application.Consumers;

public static class AcademicPeriodCreatedConsumer
{
    public static void Consume(AcademicPeriodCreated @event, IDocumentSession session)
    {
        session.Store(new AcademicPeriodView
        {
            Id = @event.AcademicPeriodId,
            InstitutionId = @event.InstitutionId,
            Name = @event.Name,
            IsActive = true
        });
    }
}
