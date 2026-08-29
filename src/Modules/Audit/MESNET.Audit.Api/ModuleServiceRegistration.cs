using MESNET.Audit.Application;
using MESNET.Audit.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Audit.Api;

public static class ModuleServiceRegistration
{
    public static IServiceCollection AddAuditModule(this IServiceCollection services)
    {
        services.AddAuditPersistence();
        services.AddAuditApplication();
        return services;
    }
}
