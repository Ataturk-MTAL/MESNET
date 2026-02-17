using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Internship.Application;

public static class ServiceRegistration
{
    /// <summary>
    /// Internship Application katmanı servislerini DI container'a ekler.
    /// Wolverine handlers otomatik keşfedilir (convention-based).
    /// Marten saga'ları register eder.
    /// </summary>
    public static IServiceCollection AddInternshipApplication(this IServiceCollection services)
    {
        // Wolverine handlers otomatik olarak keşfedilir

        // Marten saga registration (Wolverine otomatik yönetir, sadece schema config gerekli)
        services.ConfigureMarten(opts =>
        {
            opts.Schema.For<Sagas.InternshipSaga>().DatabaseSchemaName("internship");
        });

        return services;
    }
}
