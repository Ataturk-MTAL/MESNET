using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Enrollment.Application;

public static class ServiceRegistration
{
    /// <summary>
    /// Enrollment Application katmanı servislerini DI container'a ekler.
    /// Wolverine handlers otomatik keşfedilir (convention-based).
    /// </summary>
    public static IServiceCollection AddEnrollmentApplication(this IServiceCollection services)
    {
        // Wolverine handlers otomatik olarak keşfedilir
        // Custom application services burada register edilir
        
        return services;
    }
}
