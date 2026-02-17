using Microsoft.Extensions.DependencyInjection;
using MESNET.Contract.Application;
using MESNET.Contract.Persistence;

namespace MESNET.Contract.Api;

public static class ModuleServiceRegistration
{
    /// <summary>
    /// Contract modülünün tüm katmanlarını (Persistence + Application + Api) DI container'a ekler.
    /// </summary>
    public static IServiceCollection AddContractModule(this IServiceCollection services)
    {
        services.AddContractPersistence();
        services.AddContractApplication();
        services.AddContractApi();

        return services;
    }
}
