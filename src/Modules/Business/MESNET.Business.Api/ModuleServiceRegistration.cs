using Microsoft.Extensions.DependencyInjection;
using MESNET.Business.Application;
using MESNET.Business.Persistence;

namespace MESNET.Business.Api;

public static class ModuleServiceRegistration
{
    /// <summary>
    /// Business modülünün tüm katmanlarını (Persistence + Application + Api) DI container'a ekler.
    /// </summary>
    public static IServiceCollection AddBusinessModule(this IServiceCollection services)
    {
        services.AddBusinessPersistence();
        services.AddBusinessApplication();
        services.AddBusinessApi();

        return services;
    }
}
