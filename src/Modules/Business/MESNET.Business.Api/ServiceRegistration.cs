using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Business.Api;

public static class ServiceRegistration
{
    /// <summary>
    /// Business Api katmanı servislerini DI container'a ekler.
    /// Wolverine.Http endpoints otomatik keşfedilir (convention-based).
    /// </summary>
    public static IServiceCollection AddBusinessApi(this IServiceCollection services)
    {
        // Wolverine.Http endpoints otomatik olarak keşfedilir
        // Custom middleware, filters, model binders burada register edilir
        
        return services;
    }
}
