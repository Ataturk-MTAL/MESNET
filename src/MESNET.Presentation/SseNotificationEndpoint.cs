using System.Text.Json;
using MESNET.Common.Infrastructure.Notifications;
using MESNET.Common.Infrastructure.Security;

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
    /// SseUserContext'i YALNIZCA doğrulanmış JWT claim'lerinden oluşturur (CurrentUserService ile
    /// aynı kaynak/anahtarlar). Endpoint .RequireAuthorization() taşır. Kimlik alanları query string'den
    /// OKUNMAZ — aksi halde claim'siz ama kimliği doğrulanmış bir kullanıcı `?institutionId=...&roles=...`
    /// ile başka kurum/rol bildirimlerine erişebilirdi (cross-tenant/rol sızıntısı).
    /// </summary>
    private static SseUserContext ExtractSseUserContext(HttpContext http)
    {
        var user = http.User;

        var userId = Guid.TryParse(user.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty;

        var fullName = user.FindFirst("name")?.Value
                       ?? user.FindFirst("preferred_username")?.Value
                       ?? "Bilinmeyen Kullanıcı";

        var institutionId = Guid.TryParse(user.FindFirst("institution_id")?.Value, out var instId) ? instId : (Guid?)null;
        var businessId = Guid.TryParse(user.FindFirst("business_id")?.Value, out var bizId) ? bizId : (Guid?)null;
        var studentId = Guid.TryParse(user.FindFirst("student_id")?.Value, out var stuId) ? stuId : (Guid?)null;

        // Keycloak realm rolleri ClaimTypes.Role'a maplenir (CurrentUserService.cs ile aynı) —
        // kısa "role" anahtarı dolmaz, bu yüzden ClaimTypes.Role okunmalı.
        var roles = user.FindAll(System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        var permissions = user.FindAll("permissions").Select(c => c.Value).ToList();

        // Veli bağı (#174) — bu claim okunmadığı için veliye gönderilen bildirimler HİÇ
        // ulaşmıyordu (#247). Otorite claim değil kayıttır: PermissionClaimsTransformation
        // token'daki değeri silip UserAccount.LinkedStudentIds'ten yeniden yazar.
        var linkedStudentIds = LinkedStudentClaims.Read(user);

        return new SseUserContext(
            userId, fullName, institutionId, businessId, studentId, roles, permissions,
            linkedStudentIds);
    }
}
