using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Common.Infrastructure.Email;

public static class EmailServiceExtensions
{
    public static IServiceCollection AddEmailServices(this IServiceCollection services)
    {
        services.AddSingleton<IEmailTemplateService, MjmlEmailTemplateService>();
        services.AddScoped<IEmailService, MailKitEmailService>();
        return services;
    }
}
