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

    private static bool MatchesTarget(SseUserContext user, NotificationTarget target)
    {
        // Doğrudan userId eşleşme
        if (target.UserIds is { Count: > 0 } && target.UserIds.Contains(user.UserId))
            return true;

        // InstitutionId eşleşme
        if (target.InstitutionId.HasValue && user.InstitutionId == target.InstitutionId.Value)
            return true;

        // Rol eşleşme (OR: herhangi biri yeterli)
        if (target.Roles is { Count: > 0 } && user.Roles.Any(r => target.Roles.Contains(r)))
            return true;

        // Permission eşleşme
        if (!string.IsNullOrEmpty(target.RequiredPermission) && user.Permissions.Contains(target.RequiredPermission))
            return true;

        return false;
    }

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
