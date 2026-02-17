using Microsoft.Extensions.DependencyInjection;
using MESNET.Attendance.Application;
using MESNET.Attendance.Persistence;

namespace MESNET.Attendance.Api;

public static class ModuleServiceRegistration
{
    /// <summary>
    /// Attendance modülünün tüm katmanlarını (Persistence + Application + Api) DI container'a ekler.
    /// </summary>
    public static IServiceCollection AddAttendanceModule(this IServiceCollection services)
    {
        services.AddAttendancePersistence();
        services.AddAttendanceApplication();
        services.AddAttendanceApi();

        return services;
    }
}
