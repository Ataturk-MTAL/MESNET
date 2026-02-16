using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Reporting.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddReportingPersistence(this IServiceCollection services)
    {
        services.ConfigureMarten(opts =>
        {
            opts.ConfigureReportingSchema();
        });
        return services;
    }
}
