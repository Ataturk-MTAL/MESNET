using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Internship.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddInternshipPersistence(this IServiceCollection services)
    {
        services.ConfigureMarten(opts =>
        {
            opts.ConfigureInternshipSchema();
        });
        return services;
    }
}
