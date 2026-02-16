using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace MESNET.Attendance.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddAttendancePersistence(this IServiceCollection services)
    {
        services.ConfigureMarten(opts =>
        {
            opts.ConfigureAttendanceSchema();
        });
        return services;
    }
}
