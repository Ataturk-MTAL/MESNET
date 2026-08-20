using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using Wolverine;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

public static class DeleteAttendanceHandler
{
    private const int MaxDeleteDays = 7;

    /// <summary>
    /// Olay hem akışa yazılır hem <b>mesaj olarak yayınlanır</b> (#252). <c>[AggregateHandler]</c>
    /// dönüşü cascading mesaj DEĞİLDİR — gerekçe: <see cref="ApproveAttendanceHandler"/>.
    /// ///
    /// <para><b>Bu yol kesintiyi KALDIRIR</b> — <c>AbsenceTallyConsumer</c> Payment'ın
    /// <c>StudentAbsenceView</c> satırını siler. Yayınlanmazsa silinmiş devamsızlıktan para
    /// kesilmeye devam eder.</para>
    /// </summary>
    [AggregateHandler]
    public static (Events, OutgoingMessages) Handle(
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

        var deleted = new AttendanceDeleted(
            record.Id,
            record.StudentId,
            currentUser.GetFullName(),
            DateTime.UtcNow);

        return (new Events { deleted }, new OutgoingMessages { deleted });
    }
}
