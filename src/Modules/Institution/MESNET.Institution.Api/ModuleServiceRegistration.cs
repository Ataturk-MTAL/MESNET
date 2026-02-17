using Microsoft.Extensions.DependencyInjection;
using MESNET.Institution.Application;
using MESNET.Institution.Persistence;

namespace MESNET.Institution.Api;

public static class ModuleServiceRegistration
{
    /// <summary>
    /// Institution modülünün tüm katmanlarını (Persistence + Application + Api) DI container'a ekler.
    /// </summary>
    public static IServiceCollection AddInstitutionModule(this IServiceCollection services)
    {
        services.AddInstitutionPersistence();
        services.AddInstitutionApplication();
        services.AddInstitutionApi();

        return services;
    }
}
