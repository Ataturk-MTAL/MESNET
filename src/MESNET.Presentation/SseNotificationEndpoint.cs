using System.Text.Json;
using MESNET.Common.Infrastructure.Notifications;

namespace MESNET.Presentation;

public static class SseNotificationEndpoint
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static WebApplication MapSseNotificationEndpoint(this WebApplication app)
    {
        app.MapGet("/api/notifications/stream", HandleSseStream)
            .WithName("SseNotificationStream")
            .WithTags("Notifications")
            .RequireAuthorization()
            .RequireRateLimiting("SseConnections")
            .ExcludeFromDescription();

        return app;
    }

    private static async Task HandleSseStream(
        HttpContext http,
        ISseNotificationService notificationService,
        ILogger<ISseNotificationService> logger,
        CancellationToken cancellationToken)
    {
        // 1. JWT'den kullanıcı bilgilerini parse et
        var userContext = ExtractSseUserContext(http);
        if (userContext.UserId == Guid.Empty)
        {
            http.Response.StatusCode = 401;
            await http.Response.WriteAsync("Unauthorized: Geçerli bir token gerekli.", cancellationToken);
            return;
        }

        // 2. SSE response headers
        http.Response.Headers.ContentType = "text/event-stream";
        http.Response.Headers.CacheControl = "no-cache";
        http.Response.Headers.Connection = "keep-alive";
        http.Response.Headers["X-Accel-Buffering"] = "no";

        // 3. Channel'a abone ol
        var reader = notificationService.Subscribe(userContext);

        try
        {
            // 4. Başlangıç mesajı gönder (bağlantı onay)
            await WriteSseEvent(http.Response, new SseNotification(
                EventType: "connection.established",
                Module: "System",
                Payload: new { userId = userContext.UserId, connectedAt = DateTime.UtcNow },
                OccurredAt: DateTime.UtcNow
            ), cancellationToken);

            // 5. Heartbeat + notification loop
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            var heartbeatTask = SendHeartbeats(http.Response, linkedCts.Token);
            var readTask = ReadAndSendNotifications(http.Response, reader, linkedCts.Token);

            await Task.WhenAny(heartbeatTask, readTask);
            await linkedCts.CancelAsync();
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug("SSE bağlantısı istemci tarafından kapatıldı: UserId={UserId}", userContext.UserId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SSE stream hatası: UserId={UserId}", userContext.UserId);
        }
        finally
        {
            notificationService.Unsubscribe(userContext.UserId);
        }
    }

    private static async Task ReadAndSendNotifications(
        HttpResponse response,
        System.Threading.Channels.ChannelReader<SseNotification> reader,
        CancellationToken ct)
    {
        await foreach (var notification in reader.ReadAllAsync(ct))
        {
            await WriteSseEvent(response, notification, ct);
        }
    }

    private static async Task SendHeartbeats(HttpResponse response, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), ct);
            await response.WriteAsync(":keepalive\n\n", ct);
            await response.Body.FlushAsync(ct);
        }
    }

    private static async Task WriteSseEvent(
        HttpResponse response,
        SseNotification notification,
        CancellationToken ct)
    {
        await response.WriteAsync($"id:{notification.Id}\n", ct);
        await response.WriteAsync($"event:{notification.EventType}\n", ct);

        var json = JsonSerializer.Serialize(new
        {
            notification.EventType,
            notification.Module,
            notification.Payload,
            notification.OccurredAt
        }, JsonOptions);

        await response.WriteAsync($"data:{json}\n\n", ct);
        await response.Body.FlushAsync(ct);
    }

    /// <summary>
    /// JWT claim'lerinden SseUserContext oluşturur.
    /// Auth devre dışıyken dev modda query string parametreleri ile test edilebilir.
    /// </summary>
    private static SseUserContext ExtractSseUserContext(HttpContext http)
    {
        var userId = Guid.TryParse(
            http.User.FindFirst("sub")?.Value ?? http.Request.Query["userId"].FirstOrDefault(),
            out var id) ? id : Guid.Empty;

        var fullName = http.User.FindFirst("name")?.Value
                       ?? http.User.FindFirst("preferred_username")?.Value
                       ?? http.Request.Query["fullName"].FirstOrDefault()
                       ?? "Bilinmeyen Kullanıcı";

        var institutionId = Guid.TryParse(
            http.User.FindFirst("institution_id")?.Value ?? http.Request.Query["institutionId"].FirstOrDefault(),
            out var instId) ? instId : (Guid?)null;

        var businessId = Guid.TryParse(
            http.User.FindFirst("business_id")?.Value ?? http.Request.Query["businessId"].FirstOrDefault(),
            out var bizId) ? bizId : (Guid?)null;

        var studentId = Guid.TryParse(
            http.User.FindFirst("student_id")?.Value ?? http.Request.Query["studentId"].FirstOrDefault(),
            out var stuId) ? stuId : (Guid?)null;

        var roles = http.User.FindAll("role")
            .Select(c => c.Value)
            .ToList();
        if (roles.Count == 0 && http.Request.Query.TryGetValue("roles", out var queryRoles))
            roles = queryRoles.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

        var permissions = http.User.FindAll("permission")
            .Select(c => c.Value)
            .ToList();

        return new SseUserContext(userId, fullName, institutionId, businessId, studentId, roles, permissions);
    }
}
