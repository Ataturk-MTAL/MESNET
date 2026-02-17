using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Coordination.Application;

public static class ServiceRegistration
{
    /// <summary>
    /// Coordination Application katmanı servislerini DI container'a ekler.
    /// Wolverine handlers otomatik keşfedilir (convention-based).
    /// </summary>
    public static IServiceCollection AddCoordinationApplication(this IServiceCollection services)
    {
        // Wolverine handlers otomatik olarak keşfedilir
        // Custom application services burada register edilir
        
        return services;
    }
}
