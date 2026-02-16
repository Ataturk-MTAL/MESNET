using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Business.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddBusinessPersistence(this IServiceCollection services)
    {
        services.ConfigureMarten(opts =>
        {
            opts.ConfigureBusinessSchema();
        });
        return services;
    }
}
