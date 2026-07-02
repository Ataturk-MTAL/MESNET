using MESNET.Common.Infrastructure.Security;
using MESNET.Security.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Security.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddSecurityApplication(this IServiceCollection services)
    {
        services.AddScoped<IKeycloakAdminService>(sp =>
        {
            // Admin API çağrıları için yetkili (client_credentials Bearer) raw HTTP kullanılır;
            // SDK admin client'ı (IKeycloakUserClient) çalışan token EKLEMEDİĞİNDEN bırakıldı.
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("keycloak_admin_api");
            var configuration = sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<KeycloakAdminService>>();
            return new KeycloakAdminService(httpClient, configuration, logger);
        });
        services.AddScoped<IUserPermissionProvider, UserPermissionProvider>();

        return services;
    }
}
