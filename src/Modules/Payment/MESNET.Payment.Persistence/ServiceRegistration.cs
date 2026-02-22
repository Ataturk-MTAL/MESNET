using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Payment.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddPaymentPersistence(this IServiceCollection services)
    {
        services.AddSingleton<IConfigureMarten, PaymentMartenConfig>();
        return services;
    }
}
