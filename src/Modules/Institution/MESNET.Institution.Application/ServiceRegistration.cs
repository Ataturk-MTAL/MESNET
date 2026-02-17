using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Institution.Application;

public static class ServiceRegistration
{
    /// <summary>
    /// Institution Application katmanı servislerini DI container'a ekler.
    /// Wolverine handlers otomatik keşfedilir (convention-based).
    /// </summary>
    public static IServiceCollection AddInstitutionApplication(this IServiceCollection services)
    {
        // Wolverine handlers otomatik olarak keşfedilir
        // Custom application services burada register edilir
        
        return services;
    }
}
