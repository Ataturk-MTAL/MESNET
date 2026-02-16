using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Institution.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddInstitutionPersistence(this IServiceCollection services)
    {
        // Institution uses shared schema, no additional config
        return services;
    }
}
