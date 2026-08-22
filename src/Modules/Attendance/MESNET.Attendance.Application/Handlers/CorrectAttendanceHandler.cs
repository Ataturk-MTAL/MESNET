using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Core.Services;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using Wolverine;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

public static class CorrectAttendanceHandler
{
    /// <summary>
    /// Düzeltme hem akışa yazılır hem mesaj olarak yayınlanır (#252) — gerekçe:
    /// <see cref="ApproveAttendanceHandler"/>. Tür değişimi devamsızlık sınırını doldurabilir
    /// ve <c>[AggregateHandler]</c> dönüşü tek başına hiçbir handler'a ulaşmaz.
    /// </summary>
    [AggregateHandler]
    public static (Events, OutgoingMessages) Handle(
        CorrectAttendance command, AttendanceRecord? record, ICurrentUserService currentUser)
    {
        if (record is null)
            throw new DomainException("ATTENDANCE_NOT_FOUND", "Devamsızlık kaydı bulunamadı.");

        if (!record.Status.CanTransitionTo(AttendanceStatus.Corrected))
            throw new DomainException("ATTENDANCE_INVALID_STATUS",
                $"Devamsızlık kaydı bu durumdan düzeltilemez. Mevcut durum: {record.Status.Slug}.");

        if (!AbsenceType.TryFromName(command.NewAbsenceType, true, out var newType))
            throw new DomainException("ATTENDANCE_INVALID_ABSENCE_TYPE",
                $"Geçersiz devamsızlık türü: {command.NewAbsenceType}.");

        // Düzeltme de tür değiştirir; ücretli izin kısıtı burada da geçerlidir (#177).
        // Bu kapı açık kalsaydı iki taraflı onay zinciri tek komutla atlanabilirdi — #172
        // öncesinde /correct'in sağlık raporu zincirini atlaması gibi. Onaydan doğan
        // düzeltmeler bu handler'dan değil PaidLeaveAttendanceConsumer'dan geçer.
        if (AbsenceTypePolicy.RequiresApprovedRequest(newType))
            throw new DomainException(AttendanceErrors.PaidLeaveRequiresApprovedRequest());

        var corrected = new AttendanceCorrected(
            record.Id, record.StudentId, currentUser.GetFullName(),
            command.NewAbsenceType, command.Reason, DateTime.UtcNow);

        return (new Events { corrected }, new OutgoingMessages { corrected });
    }
}
