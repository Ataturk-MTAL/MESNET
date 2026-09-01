using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Audit.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddAuditPersistence(this IServiceCollection services)
    {
        services.AddSingleton<IConfigureMarten, AuditMartenConfig>();
        return services;
    }
}
