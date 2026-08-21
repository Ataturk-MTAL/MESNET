using System.Collections.Concurrent;
using System.Threading.Channels;

namespace MESNET.Common.Infrastructure.Notifications;

/// <summary>
/// Bağlı kullanıcılar için Channel yönetimi.
/// Thread-safe: ConcurrentDictionary ile birden fazla SSE bağlantısı desteklenir.
/// </summary>
internal sealed class SseConnectionManager
{
    private readonly ConcurrentDictionary<Guid, UserConnection> _connections = new();

    public (ChannelReader<SseNotification> Reader, bool IsNew) GetOrCreateChannel(SseUserContext userContext)
    {
        var connection = _connections.AddOrUpdate(
            userContext.UserId,
            _ => CreateConnection(userContext),
            (_, existing) =>
            {
                // Reconnect: eski channel kapatılır, yenisi oluşturulur
                existing.Channel.Writer.TryComplete();
                return CreateConnection(userContext);
            });

        return (connection.Channel.Reader, true);
    }

    public void RemoveConnection(Guid userId)
    {
        if (_connections.TryRemove(userId, out var connection))
            connection.Channel.Writer.TryComplete();
    }

    public IReadOnlyList<UserConnection> GetMatchingConnections(NotificationTarget target)
    {
        var results = new List<UserConnection>();

        foreach (var (_, conn) in _connections)
        {
            if (MatchesTarget(conn.UserContext, target))
                results.Add(conn);
        }

        return results;
    }

    public int ConnectionCount => _connections.Count;

    /// <summary>
    /// Karar saf <see cref="NotificationTargetPolicy"/> içindedir; burada yalnız uygulaması var.
    /// Hedefleme sessiz bir yüzeydir (boş hedef hata değil, yalnız <c>LogDebug</c>) — bu yüzden
    /// testlenebilir olmak zorunda (#247).
    /// </summary>
    private static bool MatchesTarget(SseUserContext user, NotificationTarget target)
        => NotificationTargetPolicy.Matches(user, target);

    private static UserConnection CreateConnection(SseUserContext userContext)
    {
        var channel = Channel.CreateBounded<SseNotification>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        return new UserConnection(userContext, channel);
    }

    internal sealed record UserConnection(SseUserContext UserContext, Channel<SseNotification> Channel);
}
