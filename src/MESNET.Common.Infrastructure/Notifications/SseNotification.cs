namespace MESNET.Common.Infrastructure.Notifications;

/// <summary>
/// SSE ile gönderilecek notification modeli.
/// JSON serialize edilerek "data:" alanında iletilir.
/// </summary>
public sealed record SseNotification(
    string EventType,
    string Module,
    object? Payload,
    DateTime OccurredAt)
{
    /// <summary>
    /// SSE spec id: alanı — istemci yeniden bağlandığında Last-Event-ID ile tespit
    /// </summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
}
