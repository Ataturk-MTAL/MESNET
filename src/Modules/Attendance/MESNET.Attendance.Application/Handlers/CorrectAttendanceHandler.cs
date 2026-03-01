using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

public static class CorrectAttendanceHandler
{
    [AggregateHandler]
    public static AttendanceCorrected Handle(
        CorrectAttendance command, AttendanceRecord record, ICurrentUserService currentUser)
    {
        if (!record.Status.CanTransitionTo(AttendanceStatus.Corrected))
            throw new DomainException("ATTENDANCE_INVALID_STATUS",
                $"Devamsızlık kaydı bu durumdan düzeltilemez. Mevcut durum: {record.Status.Slug}.");

        if (!AbsenceType.TryFromName(command.NewAbsenceType, true, out _))
            throw new DomainException("ATTENDANCE_INVALID_ABSENCE_TYPE",
                $"Geçersiz devamsızlık türü: {command.NewAbsenceType}.");

        return new AttendanceCorrected(
            record.Id, record.StudentId, currentUser.GetFullName(),
            command.NewAbsenceType, command.Reason, DateTime.UtcNow);
    }
}
