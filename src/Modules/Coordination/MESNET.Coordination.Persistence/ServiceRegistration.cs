using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Coordination.Persistence;

public static class ServiceRegistration
{
    /// <summary>
    /// Coordination modülünün Marten schema konfigürasyonunu kaydet
    /// </summary>
    public static IServiceCollection AddCoordinationPersistence(
        this IServiceCollection services)
    {
        services.ConfigureMarten(opts =>
        {
            opts.ConfigureCoordinationSchema();
        });

        return services;
    }
}
