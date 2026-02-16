using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Enrollment.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddEnrollmentPersistence(this IServiceCollection services)
    {
        services.ConfigureMarten(opts =>
        {
            opts.ConfigureEnrollmentSchema();
        });
        return services;
    }
}
