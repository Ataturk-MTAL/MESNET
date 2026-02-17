using Microsoft.Extensions.DependencyInjection;
using MESNET.Enrollment.Application;
using MESNET.Enrollment.Persistence;

namespace MESNET.Enrollment.Api;

public static class ModuleServiceRegistration
{
    /// <summary>
    /// Enrollment modülünün tüm katmanlarını (Persistence + Application + Api) DI container'a ekler.
    /// </summary>
    public static IServiceCollection AddEnrollmentModule(this IServiceCollection services)
    {
        services.AddEnrollmentPersistence();
        services.AddEnrollmentApplication();
        services.AddEnrollmentApi();

        return services;
    }
}
