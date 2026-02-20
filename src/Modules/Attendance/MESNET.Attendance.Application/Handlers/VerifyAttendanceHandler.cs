using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Shared;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

public static class VerifyAttendanceHandler
{
    [AggregateHandler]
    public static AttendanceVerified Handle(
        VerifyAttendance command, AttendanceRecord record)
    {
        if (!record.Status.CanTransitionTo(AttendanceStatus.Verified))
            throw new DomainException("ATTENDANCE_INVALID_STATUS",
                $"Devamsızlık kaydı bu durumdan doğrulanamaz. Mevcut durum: {record.Status.Slug}.");

        return new AttendanceVerified(record.Id, record.StudentId, command.VerifiedBy, DateTime.UtcNow);
    }
}
