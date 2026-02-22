using JasperFx.Events.Projections;
using Marten;
using Marten.Events.Projections;
using Microsoft.Extensions.DependencyInjection;
using MESNET.Attendance.Application.Projections;
using MESNET.Attendance.Core.Aggregates;

namespace MESNET.Attendance.Application;

public static class ServiceRegistration
{
    /// <summary>
    /// Attendance Application katmanı servislerini DI container'a ekler.
    /// Wolverine handlers otomatik keşfedilir (convention-based).
    /// Marten projection'ları register eder.
    /// </summary>
    public static IServiceCollection AddAttendanceApplication(this IServiceCollection services)
    {
        // Marten projections + snapshot schema
        services.ConfigureMarten(opts =>
        {
            opts.Projections.Snapshot<AttendanceRecord>(SnapshotLifecycle.Inline);
            opts.Schema.For<AttendanceRecord>().DatabaseSchemaName("attendance");
            opts.Schema.For<AttendanceRecord>().Index(x => x.StudentId);
            opts.Schema.For<AttendanceRecord>().Index(x => x.BusinessId);
            opts.Schema.For<AttendanceRecord>().Index(x => x.InstitutionId);
            opts.Schema.For<AttendanceRecord>().Index(x => x.Date);
            opts.Projections.Add<AttendanceViewProjection>(ProjectionLifecycle.Async);
        });

        return services;
    }
}
