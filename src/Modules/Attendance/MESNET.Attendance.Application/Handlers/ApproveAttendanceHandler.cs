using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using Wolverine;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

public static class ApproveAttendanceHandler
{
    /// <summary>
    /// Onay hem <b>akışa yazılır</b> hem <b>mesaj olarak yayınlanır</b> (#252).
    ///
    /// <para><b>Neden ikisi birden — <c>Events</c> tek başına YETMEZ:</b> <c>[AggregateHandler]</c>
    /// dönüşü cascading mesaj DEĞİLDİR. Wolverine.Marten bu iş akışında handler'ın dönüş
    /// eylemini <c>EventCaptureActionSource</c> ile değiştirir ve dönen nesneyi yalnız
    /// <c>IEventStream&lt;T&gt;.AppendOne</c> ile akışa ekler; hiçbir handler'a yönlendirmez.
    /// Olay yönlendirmesi (<c>EventForwardingToWolverine</c>) da açık değildir ve
    /// <b>açılmamalıdır</b> — <c>AttendanceMarked</c> zaten elle yayınlandığı için çift
    /// yayınlanır ve sınır iki kez ölçülürdü.</para>
    ///
    /// <para><b>Ölçülen sonuç:</b> bu düzeltmeden önce <c>AttendanceApproved</c> hiçbir
    /// tüketiciye ulaşmıyordu — ne devamsızlık sınırının yeniden ölçümüne, ne de Payment'ın
    /// <c>AbsenceTallyConsumer</c>'ına. Onaylanan devamsızlık ödeme tarafında süresiz
    /// <c>Pending</c> kalıyor ve <b>ücret kesintisine hiç girmiyordu</b>.</para>
    /// </summary>
    [AggregateHandler]
    public static (Events, OutgoingMessages) Handle(
        ApproveAttendance command, AttendanceRecord? record, ICurrentUserService currentUser)
    {
        if (record is null)
            throw new DomainException("ATTENDANCE_NOT_FOUND", "Devamsızlık kaydı bulunamadı.");

        if (!record.Status.CanTransitionTo(AttendanceStatus.Recorded))
            throw new DomainException("ATTENDANCE_INVALID_STATUS",
                $"Devamsızlık kaydı bu durumdan onaylanamaz. Mevcut durum: {record.Status.Slug}.");

        // Aktör kimliği saklanır, adı değil (#139).
        var approved = new AttendanceApproved(
            record.Id, record.StudentId, currentUser.GetUserId(), DateTime.UtcNow);

        return (new Events { approved }, new OutgoingMessages { approved });
    }
}
