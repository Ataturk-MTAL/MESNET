using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

public static class ApproveAttendanceHandler
{
    [AggregateHandler]
    public static AttendanceApproved Handle(
        ApproveAttendance command, AttendanceRecord? record, ICurrentUserService currentUser)
    {
        if (record is null)
            throw new DomainException("ATTENDANCE_NOT_FOUND", "Devamsızlık kaydı bulunamadı.");

        if (!record.Status.CanTransitionTo(AttendanceStatus.Recorded))
            throw new DomainException("ATTENDANCE_INVALID_STATUS",
                $"Devamsızlık kaydı bu durumdan onaylanamaz. Mevcut durum: {record.Status.Slug}.");

        // Aktör kimliği saklanır, adı değil (#139).
        return new AttendanceApproved(
            record.Id, record.StudentId, currentUser.GetUserId(), DateTime.UtcNow);
    }
}
