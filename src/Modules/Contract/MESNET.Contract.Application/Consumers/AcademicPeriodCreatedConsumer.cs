using Marten;
using MESNET.Contract.Core.ReadModels;
using MESNET.Institution.Shared.Events;

namespace MESNET.Contract.Application.Consumers;

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
