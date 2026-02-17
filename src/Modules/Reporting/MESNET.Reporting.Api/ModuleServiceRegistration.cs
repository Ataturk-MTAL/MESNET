using Microsoft.Extensions.DependencyInjection;
using MESNET.Reporting.Application;
using MESNET.Reporting.Persistence;

namespace MESNET.Reporting.Api;

public static class ModuleServiceRegistration
{
    /// <summary>
    /// Reporting modülünün tüm katmanlarını (Persistence + Application + Api) DI container'a ekler.
    /// </summary>
    public static IServiceCollection AddReportingModule(this IServiceCollection services)
    {
        services.AddReportingPersistence();
        services.AddReportingApplication();
        services.AddReportingApi();

        return services;
    }
}
