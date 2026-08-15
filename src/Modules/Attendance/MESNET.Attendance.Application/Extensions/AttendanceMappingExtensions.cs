using MESNET.Attendance.Application.Dtos;
using MESNET.Attendance.Application.Helpers;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.ValueObjects;

namespace MESNET.Attendance.Application.Extensions;

public static class AttendanceMappingExtensions
{
    /// <param name="names">
    /// Kimlik → ad sözlüğü (#139). Ad saklanmadığı için çağıran <c>UserNameResolver</c> ile
    /// çözer; sözlükte olmayan kimlik için <c>null</c> döner.
    /// </param>
    public static AttendanceRecordDto ToDto(
        this AttendanceRecord record,
        IReadOnlyDictionary<Guid, string>? names = null) => new(
        record.Id,
        record.StudentId,
        record.BusinessId,
        record.InstitutionId,
        record.Date,
        record.Type.Name,
        record.Type.Slug,
        record.Reason,
        record.Status.Name,
        record.Status.Slug,
        record.HealthReportUrl,
        record.MarkedById,
        names.NameOf(record.MarkedById),
        record.MarkedAt,
        record.ApprovedById,
        names.NameOf(record.ApprovedById),
        record.ApprovedAt,
        record.VerifiedById,
        names.NameOf(record.VerifiedById),
        record.VerifiedAt,
        record.EffectiveReportStatus.Name,
        record.EffectiveReportStatus.Slug,
        record.HealthReportAttachedById,
        names.NameOf(record.HealthReportAttachedById),
        record.HealthReportAttachedAt,
        record.HealthReportReviewedById,
        names.NameOf(record.HealthReportReviewedById),
        record.HealthReportReviewedAt,
        record.HealthReportRejectionReason);

    /// <summary>Kaydın tüm aktör kimlikleri — ad çözümü için toplu yüklemede kullanılır.</summary>
    public static IEnumerable<Guid> ActorIds(this AttendanceRecord record)
    {
        yield return record.MarkedById;
        if (record.ApprovedById is { } approved) yield return approved;
        if (record.VerifiedById is { } verified) yield return verified;
        if (record.HealthReportAttachedById is { } attached) yield return attached;
        if (record.HealthReportReviewedById is { } reviewed) yield return reviewed;
    }

    /// <param name="updatedByName">
    /// Çözümlenmiş aktör adı (#137). Ad saklanmadığı için çağıran
    /// <c>UserNameResolver</c> ile çözer; bilinmiyorsa <c>null</c> geçilir.
    /// </param>
    public static WorkCalendarDto ToDto(this WorkCalendar calendar, string? updatedByName = null) => new(
        calendar.Id,
        calendar.InstitutionId,
        calendar.Year,
        calendar.RestrictedDays.Select(d => d.ToDto()).ToList(),
        calendar.UpdatedById,
        updatedByName,
        calendar.UpdatedAt);

    public static CalendarDayDto ToDto(this CalendarDay day) => new(
        day.Date,
        day.Type.Name,
        day.Type.Slug,
        day.Description);

    public static AttendanceViewDto ToDto(this AttendanceView view) => new(
        view.Id,
        view.StudentId,
        view.AcademicPeriodId,
        view.BusinessId,
        view.TotalAbsenceDays,
        view.ExcusedDays,
        view.UnexcusedDays,
        view.LimitExceeded,
        view.LastUpdated);
}
