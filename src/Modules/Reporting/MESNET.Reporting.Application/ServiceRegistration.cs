using Microsoft.Extensions.DependencyInjection;
using QuestPDF.Infrastructure;

namespace MESNET.Reporting.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddReportingApplication(this IServiceCollection services)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return services;
    }
}
