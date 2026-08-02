using Marten;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using Wolverine.Marten;

namespace MESNET.Attendance.Application.Handlers;

public static class CorrectAttendanceHandler
{
    [AggregateHandler]
    public static async Task<AttendanceCorrected> Handle(
        CorrectAttendance command, AttendanceRecord? record,
        ICurrentUserService currentUser, IQuerySession session)
    {
        if (record is null)
            throw new DomainException("ATTENDANCE_NOT_FOUND", "Devamsızlık kaydı bulunamadı.");

        if (!record.Status.CanTransitionTo(AttendanceStatus.Corrected))
            throw new DomainException("ATTENDANCE_INVALID_STATUS",
                $"Devamsızlık kaydı bu durumdan düzeltilemez. Mevcut durum: {record.Status.Slug}.");

        if (!AbsenceType.TryFromName(command.NewAbsenceType, true, out var newType))
            throw new DomainException("ATTENDANCE_INVALID_ABSENCE_TYPE",
                $"Geçersiz devamsızlık türü: {command.NewAbsenceType}.");

        // Düzeltme de tür değiştirir; ücretli izin kısıtı burada da geçerlidir (#175).
        // Uç zaten "attendance:direct-entry" istiyor (#172), yani okul tarafındayız —
        // bildirim kısıtı (CanReport) burada uygulanmaz, eğitim türü kısıtı uygulanır.
        await MarkAttendanceHandler.EnsureTypeAllowedForStudentAsync(session, record.StudentId, newType);

        return new AttendanceCorrected(
            record.Id, record.StudentId, currentUser.GetFullName(),
            command.NewAbsenceType, command.Reason, DateTime.UtcNow);
    }
}
