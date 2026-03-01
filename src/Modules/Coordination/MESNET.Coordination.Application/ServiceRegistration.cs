using MESNET.Coordination.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Coordination.Application;

public static class ServiceRegistration
{
    /// <summary>
    /// Coordination Application katmanı servislerini DI container'a ekler.
    /// Wolverine handlers otomatik keşfedilir (convention-based).
    /// </summary>
    public static IServiceCollection AddCoordinationApplication(this IServiceCollection services,
        IConfiguration configuration)
    {
        // OSRM — rota bazlı mesafe hesaplama servisi
        var osrmBaseUrl = configuration["Osrm:BaseUrl"] ?? "http://localhost:5000";
        services.AddHttpClient<IOsrmDistanceService, OsrmDistanceService>(client =>
        {
            client.BaseAddress = new Uri(osrmBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}
