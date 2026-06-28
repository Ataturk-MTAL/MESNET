using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Shared;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

public static class AttachHealthReportHandler
{
    [AggregateHandler]
    public static HealthReportAttached Handle(
        AttachHealthReport command, AttendanceRecord? record)
    {
        if (record is null)
            throw new DomainException("ATTENDANCE_NOT_FOUND", "Devamsızlık kaydı bulunamadı.");

        return new HealthReportAttached(record.Id, record.StudentId, command.ReportUrl, DateTime.UtcNow);
    }
}
