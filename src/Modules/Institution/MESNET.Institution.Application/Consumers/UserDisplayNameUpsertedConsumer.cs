using Marten;
using MESNET.Institution.Core.ReadModels;
using MESNET.Security.Shared.Events;

namespace MESNET.Institution.Application.Consumers;

/// <summary>
/// Security'den gelen <c>UserDisplayNameUpserted</c> olayını dinleyip lokal
/// <c>UserNameView</c>'ı günceller (#137). Denetim alanlarının adı bu view'dan çözülür.
/// </summary>
public static class UserDisplayNameUpsertedConsumer
{
    public static void Consume(UserDisplayNameUpserted @event, IDocumentSession session)
    {
        session.Store(new UserNameView
        {
            Id = @event.UserId,
            FullName = @event.FullName
        });
    }
}
