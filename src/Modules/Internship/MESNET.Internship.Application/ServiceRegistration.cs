using Marten;
using MESNET.Common.Infrastructure.Deployment;
using MESNET.Internship.Application.Deployment;
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

        // Dağıtım ön koşulu sondası — kopya saga'ları açılışta ÖLÇER (#248, #251).
        services.AddScoped<IDeploymentPrerequisiteProbe, InternshipSagaDuplicateProbe>();

        return services;
    }
}
