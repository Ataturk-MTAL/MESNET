using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Institution.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddInstitutionPersistence(this IServiceCollection services)
    {
        services.AddSingleton<IConfigureMarten, InstitutionMartenConfig>();
        return services;
    }
}
