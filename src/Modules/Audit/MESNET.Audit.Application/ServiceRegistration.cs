using MESNET.Audit.Application.Auditing;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Audit.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddAuditApplication(this IServiceCollection services)
    {
        services.AddScoped<AuditContextAccessor>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddHostedService<Services.AuditRetentionService>();
        return services;
    }
}
