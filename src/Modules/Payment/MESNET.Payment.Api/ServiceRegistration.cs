using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Payment.Api;

public static class ServiceRegistration
{
    /// <summary>
    /// Payment Api katmanı servislerini DI container'a ekler.
    /// Wolverine.Http endpoints otomatik keşfedilir (convention-based).
    /// </summary>
    public static IServiceCollection AddPaymentApi(this IServiceCollection services)
    {
        // Wolverine.Http endpoints otomatik olarak keşfedilir
        // Custom middleware, filters, model binders burada register edilir
        
        return services;
    }
}
