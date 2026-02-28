using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Common.Infrastructure.Email;

public static class EmailServiceExtensions
{
    public static IServiceCollection AddEmailTemplateService(this IServiceCollection services)
    {
        services.AddSingleton<IEmailTemplateService, MjmlEmailTemplateService>();
        return services;
    }
}
