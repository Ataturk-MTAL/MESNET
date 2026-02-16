using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

public static class VerifyAttendanceHandler
{
    [AggregateHandler]
    public static AttendanceVerified Handle(
        VerifyAttendance command, AttendanceRecord record)
    {
        if (!record.Status.CanTransitionTo(AttendanceStatus.Verified))
            throw new InvalidOperationException(
                $"Devamsızlık kaydı {record.Status.Name} durumundan doğrulanamaz.");

        return new AttendanceVerified(
            record.Id, record.StudentId, command.VerifiedBy, DateTime.UtcNow);
    }
}
