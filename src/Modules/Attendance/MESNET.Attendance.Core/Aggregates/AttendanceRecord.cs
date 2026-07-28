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
    /// <summary>
    /// Denetim aktörlerinin kimlikleri — token'dan gelir, ad SAKLANMAZ (#139).
    /// Ad okuma anında <c>UserNameView</c>'dan çözülür; böylece kullanıcı adını
    /// değiştirince eski kayıtlar bayat ad göstermez ve aktöre göre sorgu yapılabilir.
    /// Eski <c>markedBy</c>/<c>approvedBy</c>/<c>verifiedBy</c> JSON anahtarları (serbest
    /// metin ad) bu adlarla artık okunmaz.
    /// </summary>
    Guid MarkedById,
    DateTime MarkedAt,
    Guid? ApprovedById,
    DateTime? ApprovedAt,
    Guid? VerifiedById,
    DateTime? VerifiedAt,
    bool IsDeleted = false)
{
    // SmartEnum LINQ tuzağı: Status JSON'a düz string serialize edilir; sorgular için düz string kopya.
    public string StatusName => Status.Name;

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
        e.MarkedById,
        DateTime.UtcNow,
        null,
        null,
        null,
        null);

    public AttendanceRecord Apply(AttendanceApproved e) => this with
    {
        Status = AttendanceStatus.Recorded,
        ApprovedById = e.ApprovedById,
        ApprovedAt = e.ApprovedAt
    };

    public AttendanceRecord Apply(AttendanceVerified e) => this with
    {
        Status = AttendanceStatus.Verified,
        VerifiedById = e.VerifiedById,
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
