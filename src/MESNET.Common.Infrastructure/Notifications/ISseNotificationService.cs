using System.Threading.Channels;

namespace MESNET.Common.Infrastructure.Notifications;

/// <summary>
/// SSE notification publish servisi.
/// Wolverine handler'lar bu interface'i inject ederek notification gönderir.
/// </summary>
public interface ISseNotificationService
{
    /// <summary>
    /// Hedef kullanıcılara notification gönderir.
    /// Target kriterlerine uyan tüm bağlı kullanıcılara iletilir.
    /// </summary>
    Task PublishAsync(SseNotification notification, NotificationTarget target, CancellationToken ct = default);

    /// <summary>
    /// Kullanıcı için SSE channel oluşturur veya mevcut olanı döndürür.
    /// SSE endpoint tarafından çağrılır.
    /// </summary>
    ChannelReader<SseNotification> Subscribe(SseUserContext userContext);

    /// <summary>
    /// Kullanıcı bağlantısını sonlandırır.
    /// SSE endpoint request iptal olduğunda çağrılır.
    /// </summary>
    void Unsubscribe(Guid userId);

    /// <summary>
    /// Aktif bağlantı sayısını döndürür (monitoring için).
    /// </summary>
    int ActiveConnectionCount { get; }
}
