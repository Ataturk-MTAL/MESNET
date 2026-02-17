using Microsoft.Extensions.DependencyInjection;
using MESNET.Coordination.Application;
using MESNET.Coordination.Persistence;

namespace MESNET.Coordination.Api;

public static class ModuleServiceRegistration
{
    /// <summary>
    /// Coordination modülünün tüm katmanlarını (Persistence + Application + Api) DI container'a ekler.
    /// </summary>
    public static IServiceCollection AddCoordinationModule(this IServiceCollection services)
    {
        services.AddCoordinationPersistence();
        services.AddCoordinationApplication();
        services.AddCoordinationApi();

        return services;
    }
}
