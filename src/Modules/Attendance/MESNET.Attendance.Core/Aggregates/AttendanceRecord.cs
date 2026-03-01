using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Shared.Events;

namespace MESNET.Attendance.Core.Aggregates;

public sealed record AttendanceRecord(
    Guid Id,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    DateTime Date,
    AbsenceType Type,
    string? Reason,
    AttendanceStatus Status,
    string? HealthReportUrl,
    string MarkedBy,
    DateTime MarkedAt,
    string? ApprovedBy,
    DateTime? ApprovedAt,
    string? VerifiedBy,
    DateTime? VerifiedAt,
    bool IsDeleted = false)
{
    public static AttendanceRecord Create(AttendanceMarked e) => new(
        e.AttendanceId,
        e.StudentId,
        e.BusinessId,
        e.InstitutionId,
        e.AcademicPeriodId,
        e.Date,
        AbsenceType.TryFromName(e.AbsenceType, true, out var type) ? type : AbsenceType.Unexcused,
        null,
        AttendanceStatus.TryFromName(e.InitialStatus, true, out var status)
            ? status : AttendanceStatus.Recorded,
        null,
        e.MarkedBy ?? string.Empty,
        DateTime.UtcNow,
        null,
        null,
        null,
        null);

    public AttendanceRecord Apply(AttendanceApproved e) => this with
    {
        Status = AttendanceStatus.Recorded,
        ApprovedBy = e.ApprovedBy,
        ApprovedAt = e.ApprovedAt
    };

    public AttendanceRecord Apply(AttendanceVerified e) => this with
    {
        Status = AttendanceStatus.Verified,
        VerifiedBy = e.VerifiedBy,
        VerifiedAt = e.VerifiedAt
    };

    public AttendanceRecord Apply(AttendanceCorrected e) => this with
    {
        Status = AttendanceStatus.Corrected,
        Type = AbsenceType.TryFromName(e.NewAbsenceType, true, out var type) ? type : Type,
        Reason = e.Reason
    };

    public AttendanceRecord Apply(HealthReportAttached e) => this with
    {
        HealthReportUrl = e.ReportUrl,
        Type = AbsenceType.HealthReport
    };

    public AttendanceRecord Apply(AttendanceDeleted _) => this with
    {
        IsDeleted = true
    };
}
