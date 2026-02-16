using JasperFx.Events.Projections;
using Marten;
using Marten.Events.Projections;
using MESNET.Attendance.Application.Projections;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Entities;

namespace MESNET.Attendance.Persistence;

public class AttendanceMartenConfig : IConfigureMarten
{
    public void Configure(IServiceProvider services, StoreOptions options)
    {
        options.Projections.Snapshot<AttendanceRecord>(SnapshotLifecycle.Inline);

        options.Projections.Add<AttendanceViewProjection>(ProjectionLifecycle.Async);

        options.Schema.For<AttendanceRecord>().DatabaseSchemaName("attendance");
        options.Schema.For<AttendanceRecord>().Index(x => x.StudentId);
        options.Schema.For<AttendanceRecord>().Index(x => x.BusinessId);
        options.Schema.For<AttendanceRecord>().Index(x => x.InstitutionId);
        options.Schema.For<AttendanceRecord>().Index(x => x.Date);

        options.Schema.For<WorkCalendar>().DatabaseSchemaName("attendance");
        options.Schema.For<WorkCalendar>().Index(x => x.InstitutionId);

        options.Schema.For<AttendanceView>().DatabaseSchemaName("attendance");
        options.Schema.For<AttendanceView>().Index(x => x.StudentId);
        options.Schema.For<AttendanceView>().Index(x => x.BusinessId);
    }
}
