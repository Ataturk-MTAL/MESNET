using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Business.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddBusinessPersistence(this IServiceCollection services)
    {
        services.AddSingleton<IConfigureMarten, BusinessMartenConfig>();
        return services;
    }
}
