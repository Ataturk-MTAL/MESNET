using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Internship.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddInternshipPersistence(this IServiceCollection services)
    {
        services.AddSingleton<IConfigureMarten, InternshipMartenConfig>();
        return services;
    }
}
