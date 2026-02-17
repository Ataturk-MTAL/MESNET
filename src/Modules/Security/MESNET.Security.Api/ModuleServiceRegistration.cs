using MESNET.Security.Application;
using MESNET.Security.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Security.Api;

public static class ModuleServiceRegistration
{
    public static IServiceCollection AddSecurityModule(this IServiceCollection services)
    {
        services.AddSecurityPersistence();
        services.AddSecurityApplication();
        services.AddSecurityApi();

        return services;
    }
}
