using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Shared;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

public static class AttachHealthReportHandler
{
    [AggregateHandler]
    public static (Result, HealthReportAttached?) Handle(
        AttachHealthReport command, AttendanceRecord record)
    {
        return (Result.Success(), new HealthReportAttached(
            record.Id, record.StudentId, command.ReportUrl, DateTime.UtcNow));
    }
}
