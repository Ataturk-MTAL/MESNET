using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MESNET.Common.Infrastructure.Notifications;

public static class NotificationServiceExtensions
{
    /// <summary>
    /// SSE notification altyapısını DI container'a ekler.
    /// Singleton: tüm kullanıcı bağlantıları tek instance'da yönetilir.
    /// </summary>
    public static IHostApplicationBuilder AddSseNotifications(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton<SseConnectionManager>();
        builder.Services.AddSingleton<ISseNotificationService, SseNotificationService>();

        return builder;
    }
}
