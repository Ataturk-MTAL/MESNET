using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Shared.Events;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

public static class CorrectAttendanceHandler
{
    [AggregateHandler]
    public static AttendanceCorrected Handle(
        CorrectAttendance command, AttendanceRecord record)
    {
        if (!record.Status.CanTransitionTo(AttendanceStatus.Corrected))
            throw new InvalidOperationException(
                $"Devamsızlık kaydı {record.Status.Name} durumundan düzeltilemez.");

        if (!AbsenceType.TryFromName(command.NewAbsenceType, true, out _))
            throw new InvalidOperationException(
                $"Geçersiz devamsızlık türü: {command.NewAbsenceType}");

        return new AttendanceCorrected(
            record.Id, record.StudentId, command.CorrectedBy,
            command.NewAbsenceType, command.Reason, DateTime.UtcNow);
    }
}
