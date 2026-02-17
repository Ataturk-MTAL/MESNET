using Microsoft.Extensions.DependencyInjection;
using MESNET.Payment.Application;
using MESNET.Payment.Persistence;

namespace MESNET.Payment.Api;

public static class ModuleServiceRegistration
{
    /// <summary>
    /// Payment modülünün tüm katmanlarını (Persistence + Application + Api) DI container'a ekler.
    /// </summary>
    public static IServiceCollection AddPaymentModule(this IServiceCollection services)
    {
        services.AddPaymentPersistence();
        services.AddPaymentApplication();
        services.AddPaymentApi();

        return services;
    }
}
