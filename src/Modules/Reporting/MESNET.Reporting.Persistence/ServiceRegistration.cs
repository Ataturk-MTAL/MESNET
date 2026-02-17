using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Reporting.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddReportingPersistence(this IServiceCollection services)
    {
        // Schema ve index konfigürasyonu ReportingMartenConfig : IConfigureMarten ile yapılır
        return services;
    }
}
