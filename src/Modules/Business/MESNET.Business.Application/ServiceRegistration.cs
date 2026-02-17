using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Business.Application;

public static class ServiceRegistration
{
    /// <summary>
    /// Business Application katmanı servislerini DI container'a ekler.
    /// Wolverine handlers otomatik keşfedilir (convention-based).
    /// </summary>
    public static IServiceCollection AddBusinessApplication(this IServiceCollection services)
    {
        // Wolverine handlers otomatik olarak keşfedilir
        // Custom application services burada register edilir
        
        return services;
    }
}
