using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

public static class AttachHealthReportHandler
{
    [AggregateHandler]
    public static HealthReportAttached Handle(
        AttachHealthReport command, AttendanceRecord record)
    {
        return new HealthReportAttached(
            record.Id, record.StudentId, command.ReportUrl, DateTime.UtcNow);
    }
}
