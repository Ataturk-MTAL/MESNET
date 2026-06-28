using Marten;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Institution.Shared.Events;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// Institution.InstitutionUpdated → Coordination'ın InstitutionView read model'ini günceller.
/// Kurum oluşturma, güncelleme ve ders programı ayarı değişiminde yayınlanır.
/// </summary>
public static class InstitutionUpdatedConsumer
{
    public static void Consume(InstitutionUpdated @event, IDocumentSession session)
    {
        session.Store(new InstitutionView
        {
            Id = @event.InstitutionId,
            FullName = @event.FullName,
            Location = @event.Location,
            DailyPeriodCount = @event.DailyPeriodCount
        });
    }
}
