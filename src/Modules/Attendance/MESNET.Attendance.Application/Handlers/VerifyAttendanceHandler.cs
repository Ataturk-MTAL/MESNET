using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Shared;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

public static class VerifyAttendanceHandler
{
    [AggregateHandler]
    public static Result<AttendanceVerified> Handle(
        VerifyAttendance command, AttendanceRecord record)
    {
        if (!record.Status.CanTransitionTo(AttendanceStatus.Verified))
            return Result<AttendanceVerified>.Failure(
                AttendanceErrors.InvalidStatus(record.Id, record.Status.Slug,
                    "Devamsızlık kaydı bu durumdan doğrulanamaz."));

        return Result<AttendanceVerified>.Success(new AttendanceVerified(
            record.Id, record.StudentId, command.VerifiedBy, DateTime.UtcNow));
    }
}
