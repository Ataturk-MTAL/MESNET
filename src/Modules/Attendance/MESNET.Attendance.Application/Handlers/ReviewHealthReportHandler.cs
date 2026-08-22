using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using Wolverine;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

/// <summary>
/// Sağlık raporu onay/ret adımı (#172) — zincirin 1. adımı, <c>attendance:approve</c> ister.
/// Bu izin koordinatör öğretmen, müdür yardımcısı ve müdürdedir; işletme rollerinde YOKTUR.
/// </summary>
public static class ApproveHealthReportHandler
{
    /// <summary>
    /// Olay hem akışa yazılır hem <b>mesaj olarak yayınlanır</b> (#252). <c>[AggregateHandler]</c>
    /// dönüşü cascading mesaj DEĞİLDİR — gerekçe: <see cref="ApproveAttendanceHandler"/>.
    /// ///
    /// <para><b>Bu yol kesintiyi KALDIRIR</b> — <c>AbsenceTallyConsumer</c> türü
    /// <c>HealthReport</c>'a çevirir ve o tür ücret kesintisine tabi değildir (#172). Onay
    /// yayınlanmazken kesintiyi <i>koyan</i> yol yayınlanırsa, geçerli raporu olan öğrencinin
    /// ücreti kesilir ve hiçbir arayüzden geri alınamaz.</para>
    /// </summary>
    [AggregateHandler]
    public static (Events, OutgoingMessages) Handle(
        ApproveHealthReport command, AttendanceRecord? record, ICurrentUserService currentUser)
    {
        var target = EnsureReviewable(command.AttendanceId, record);

        var approved = new HealthReportApproved(
            target.Id, target.StudentId, currentUser.GetUserId(), DateTime.UtcNow);

        return (new Events { approved }, new OutgoingMessages { approved });
    }

    /// <summary>Rapor var mı ve onay bekliyor mu. Zaten onaylanmış rapor ikinci kez işlenemez.</summary>
    internal static AttendanceRecord EnsureReviewable(Guid attendanceId, AttendanceRecord? record)
    {
        if (record is null)
            throw new DomainException(AttendanceErrors.NotFound(attendanceId));

        if (record.HealthReportUrl is null)
            throw new DomainException(AttendanceErrors.HealthReportMissing(attendanceId));

        if (!record.EffectiveReportStatus.CanReview)
            throw new DomainException(
                AttendanceErrors.HealthReportNotPending(record.EffectiveReportStatus.Slug));

        return record;
    }
}

public static class RejectHealthReportHandler
{
    [AggregateHandler]
    public static HealthReportRejected Handle(
        RejectHealthReport command, AttendanceRecord? record, ICurrentUserService currentUser)
    {
        if (string.IsNullOrWhiteSpace(command.Reason))
            throw new DomainException(AttendanceErrors.RejectionReasonRequired());

        var target = ApproveHealthReportHandler.EnsureReviewable(command.AttendanceId, record);

        return new HealthReportRejected(
            target.Id, target.StudentId, currentUser.GetUserId(), DateTime.UtcNow, command.Reason.Trim());
    }
}
