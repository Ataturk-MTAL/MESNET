using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Contract.Application;

public static class ServiceRegistration
{
    /// <summary>
    /// Contract Application katmanı servislerini DI container'a ekler.
    /// Wolverine handlers otomatik keşfedilir (convention-based).
    /// </summary>
    public static IServiceCollection AddContractApplication(this IServiceCollection services)
    {
        // Wolverine handlers otomatik olarak keşfedilir
        // Custom application services burada register edilir
        
        return services;
    }
}
