using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace MESNET.Common.Infrastructure.Notifications;

/// <summary>
/// In-memory Channel&lt;T&gt; tabanlı SSE notification servisi.
/// Phase 1: tek instance (modular monolith). Phase 2: Redis Pub/Sub ile scale-out.
/// </summary>
internal sealed class SseNotificationService : ISseNotificationService
{
    private readonly SseConnectionManager _connectionManager;
    private readonly ILogger<SseNotificationService> _logger;

    public SseNotificationService(
        SseConnectionManager connectionManager,
        ILogger<SseNotificationService> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public Task PublishAsync(
        SseNotification notification,
        NotificationTarget target,
        CancellationToken ct = default)
    {
        // Geniş ölçüt (rol/izin) kiracı daraltması olmadan verilmişse hedef KİMSEYE ulaşmaz
        // (#266) — sızdırmaktansa göndermemek doğrudur, ama bu neredeyse her zaman çağıranın
        // hatasıdır ve sessiz kalmamalı.
        if (target.LeaksWithoutInstitutionScope)
        {
            _logger.LogWarning(
                "Bildirim hedefi KİRACI DARALTMASI OLMADAN geniş ölçüt taşıyor ve kimseye "
                + "ulaşmayacak: EventType={EventType}, Module={Module}. Rol/izin hedefleri "
                + "InstitutionId ile birlikte verilmelidir (#266).",
                notification.EventType, notification.Module);
        }

        var connections = _connectionManager.GetMatchingConnections(target);

        if (connections.Count == 0)
        {
            _logger.LogDebug(
                "Notification hedef kitlesi için aktif bağlantı yok: EventType={EventType}, Module={Module}",
                notification.EventType, notification.Module);
            return Task.CompletedTask;
        }

        var writeCount = 0;
        foreach (var conn in connections)
        {
            try
            {
                if (conn.Channel.Writer.TryWrite(notification))
                {
                    writeCount++;
                }
                else
                {
                    _logger.LogWarning(
                        "Channel dolu, notification atıldı: UserId={UserId}, EventType={EventType}",
                        conn.UserContext.UserId, notification.EventType);
                }
            }
            catch (ChannelClosedException)
            {
                _logger.LogDebug("Channel kapalı, kullanıcı disconnected: UserId={UserId}", conn.UserContext.UserId);
            }
        }

        _logger.LogInformation(
            "Notification gönderildi: EventType={EventType}, Module={Module}, SentTo={Count}",
            notification.EventType, notification.Module, writeCount);

        return Task.CompletedTask;
    }

    public ChannelReader<SseNotification> Subscribe(SseUserContext userContext)
    {
        var (reader, _) = _connectionManager.GetOrCreateChannel(userContext);

        _logger.LogInformation(
            "SSE bağlantısı açıldı: UserId={UserId}, FullName={FullName}, Roles={Roles}",
            userContext.UserId, userContext.FullName, string.Join(",", userContext.Roles));

        return reader;
    }

    public void Unsubscribe(Guid userId)
    {
        _connectionManager.RemoveConnection(userId);

        _logger.LogInformation("SSE bağlantısı kapatıldı: UserId={UserId}", userId);
    }

    public int ActiveConnectionCount => _connectionManager.ConnectionCount;
}
