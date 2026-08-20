using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using Wolverine;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

public static class VerifyAttendanceHandler
{
    /// <summary>
    /// Olay hem akışa yazılır hem <b>mesaj olarak yayınlanır</b> (#252). <c>[AggregateHandler]</c>
    /// dönüşü cascading mesaj DEĞİLDİR — gerekçe: <see cref="ApproveAttendanceHandler"/>.
    /// ///
    /// <para>Payment'ın yerel kaydındaki durumu günceller; durum ekseni iki tarafta
    /// ayrışmamalı.</para>
    /// </summary>
    [AggregateHandler]
    public static (Events, OutgoingMessages) Handle(
        VerifyAttendance command, AttendanceRecord? record, ICurrentUserService currentUser)
    {
        if (record is null)
            throw new DomainException("ATTENDANCE_NOT_FOUND", "Devamsızlık kaydı bulunamadı.");

        if (!record.Status.CanTransitionTo(AttendanceStatus.Verified))
            throw new DomainException("ATTENDANCE_INVALID_STATUS",
                $"Devamsızlık kaydı bu durumdan doğrulanamaz. Mevcut durum: {record.Status.Slug}.");

        // Aktör kimliği saklanır, adı değil (#139).
        var verified = new AttendanceVerified(
            record.Id, record.StudentId, currentUser.GetUserId(), DateTime.UtcNow);

        return (new Events { verified }, new OutgoingMessages { verified });
    }
}
