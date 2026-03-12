using MESNET.Reporting.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace MESNET.Reporting.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddReportingApplication(this IServiceCollection services)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        // Ay sonu otomatik devamsızlık raporu üretimi
        services.AddHostedService<MonthlyReportSchedulerService>();

        return services;
    }
}
