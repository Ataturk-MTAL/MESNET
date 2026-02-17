using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Security.Api;

public static class ServiceRegistration
{
    public static IServiceCollection AddSecurityApi(this IServiceCollection services)
    {
        // Wolverine.Http endpoints otomatik olarak keşfedilir
        return services;
    }
}
