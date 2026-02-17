using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Security.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddSecurityPersistence(this IServiceCollection services)
    {
        services.ConfigureMarten(opts =>
        {
            opts.ConfigureSecuritySchema();
        });
        return services;
    }
}
