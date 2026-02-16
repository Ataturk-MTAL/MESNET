using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Payment.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddPaymentPersistence(this IServiceCollection services)
    {
        services.ConfigureMarten(opts =>
        {
            opts.ConfigurePaymentSchema();
        });
        return services;
    }
}
