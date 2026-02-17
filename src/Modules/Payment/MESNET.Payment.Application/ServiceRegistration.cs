using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Payment.Application;

public static class ServiceRegistration
{
    /// <summary>
    /// Payment Application katmanı servislerini DI container'a ekler.
    /// Wolverine handlers otomatik keşfedilir (convention-based).
    /// Marten saga'ları register eder.
    /// </summary>
    public static IServiceCollection AddPaymentApplication(this IServiceCollection services)
    {
        // Wolverine handlers otomatik olarak keşfedilir

        // Marten saga registration (Wolverine otomatik yönetir, sadece schema config gerekli)
        services.ConfigureMarten(opts =>
        {
            opts.Schema.For<Sagas.PaymentSaga>().DatabaseSchemaName("payment");
        });

        return services;
    }
}
