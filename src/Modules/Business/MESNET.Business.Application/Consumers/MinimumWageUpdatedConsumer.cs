using Marten;
using MESNET.Business.Application.ReadModels;
using MESNET.Institution.Shared.Events;

namespace MESNET.Business.Application.Consumers;

public static class MinimumWageUpdatedConsumer
{
    public static void Consume(MinimumWageUpdated @event, IDocumentSession session)
    {
        var reference = new MinimumWageReference
        {
            Id = GuidHelper.Combine(@event.InstitutionId, "MinimumWage"),
            InstitutionId = @event.InstitutionId,
            Amount = @event.Amount,
            EffectiveDate = @event.EffectiveDate,
            LastUpdated = DateTime.UtcNow
        };

        session.Store(reference);
    }
}
