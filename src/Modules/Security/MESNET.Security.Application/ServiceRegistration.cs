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
            var userClient = sp.GetRequiredService<Keycloak.AuthServices.Sdk.Admin.IKeycloakUserClient>();
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient("keycloak_admin_api");
            var configuration = sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<KeycloakAdminService>>();
            return new KeycloakAdminService(userClient, httpClient, configuration, logger);
        });
        services.AddScoped<IEmailService, SmtpEmailService>();
        services.AddScoped<IUserPermissionProvider, UserPermissionProvider>();

        return services;
    }
}
