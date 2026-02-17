using Microsoft.Extensions.DependencyInjection;
using MESNET.Internship.Application;
using MESNET.Internship.Persistence;

namespace MESNET.Internship.Api;

public static class ModuleServiceRegistration
{
    /// <summary>
    /// Internship modülünün tüm katmanlarını (Persistence + Application + Api) DI container'a ekler.
    /// </summary>
    public static IServiceCollection AddInternshipModule(this IServiceCollection services)
    {
        services.AddInternshipPersistence();
        services.AddInternshipApplication();
        services.AddInternshipApi();

        return services;
    }
}
