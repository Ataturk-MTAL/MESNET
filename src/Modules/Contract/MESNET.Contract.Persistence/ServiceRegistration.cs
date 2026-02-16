using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Contract.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddContractPersistence(this IServiceCollection services)
    {
        services.ConfigureMarten(opts =>
        {
            opts.ConfigureContractSchema();
        });
        return services;
    }
}
