using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Shared;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

public static class CorrectAttendanceHandler
{
    [AggregateHandler]
    public static (Result, AttendanceCorrected?) Handle(
        CorrectAttendance command, AttendanceRecord record)
    {
        if (!record.Status.CanTransitionTo(AttendanceStatus.Corrected))
            return (Result.Failure(
                AttendanceErrors.InvalidStatus(record.Id, record.Status.Slug,
                    "Devamsızlık kaydı bu durumdan düzeltilemez.")), null);

        if (!AbsenceType.TryFromName(command.NewAbsenceType, true, out _))
            return (Result.Failure(
                AttendanceErrors.OperationFailed("Correct", $"Geçersiz devamsızlık türü: {command.NewAbsenceType}")), null);

        return (Result.Success(), new AttendanceCorrected(
            record.Id, record.StudentId, command.CorrectedBy,
            command.NewAbsenceType, command.Reason, DateTime.UtcNow));
    }
}
