using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Institution.Application.Services;
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

        // Kiracı listesi okul kayıtlarından üretilir (#149). Sözleşme altyapıda, uygulama
        // burada — arka plan işleri "hangi kiracı adına" sorusunu bu servisle cevaplar.
        services.AddScoped<ITenantDirectory, InstitutionTenantDirectory>();

        // Alt ağaç kiracı listesi (D2). Aynı gerekçe: sözleşme altyapıda, uygulama burada.
        services.AddScoped<IInstitutionSubtreeDirectory, InstitutionSubtreeDirectory>();

        return services;
    }
}
