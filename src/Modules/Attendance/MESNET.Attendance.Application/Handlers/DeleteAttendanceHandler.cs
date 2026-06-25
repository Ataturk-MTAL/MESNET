using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

public static class DeleteAttendanceHandler
{
    private const int MaxDeleteDays = 7;

    [AggregateHandler]
    public static AttendanceDeleted Handle(
        DeleteAttendance command, AttendanceRecord? record, ICurrentUserService currentUser)
    {
        if (record is null)
            throw new DomainException("ATTENDANCE_NOT_FOUND", "Devamsızlık kaydı bulunamadı.");

        if (record.IsDeleted)
            throw new DomainException("ATTENDANCE_ALREADY_DELETED",
                "Bu devamsızlık kaydı zaten silinmiş.");

        var daysSinceRecord = (DateTime.UtcNow.Date - record.Date.Date).Days;
        if (daysSinceRecord > MaxDeleteDays)
            throw new DomainException("ATTENDANCE_DELETE_EXPIRED",
                $"Devamsızlık kaydı yalnızca son {MaxDeleteDays} gün içinde silinebilir. Kayıt tarihi: {record.Date:dd.MM.yyyy}");

        return new AttendanceDeleted(
            record.Id,
            record.StudentId,
            currentUser.GetFullName(),
            DateTime.UtcNow);
    }
}
