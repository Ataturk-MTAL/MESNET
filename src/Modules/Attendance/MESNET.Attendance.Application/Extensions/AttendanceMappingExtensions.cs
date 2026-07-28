using MESNET.Attendance.Application.Dtos;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.ValueObjects;

namespace MESNET.Attendance.Application.Extensions;

public static class AttendanceMappingExtensions
{
    public static AttendanceRecordDto ToDto(this AttendanceRecord record) => new(
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
        record.MarkedBy,
        record.MarkedAt,
        record.ApprovedBy,
        record.ApprovedAt,
        record.VerifiedBy,
        record.VerifiedAt);

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
        view.BusinessId,
        view.TotalAbsenceDays,
        view.ExcusedDays,
        view.UnexcusedDays,
        view.LimitExceeded,
        view.LastUpdated);
}
