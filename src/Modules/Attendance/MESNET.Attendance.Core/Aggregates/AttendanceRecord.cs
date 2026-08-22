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
    bool IsDeleted = false,
    /// <summary>
    /// Sağlık raporunun onay durumu (#172). Rapor eklemek devamsızlık türünü ANINDA değiştirmez;
    /// tür yalnız <c>Approved</c> olduğunda <c>HealthReport</c>'a döner.
    ///
    /// <para>Alan #172 ile eklendiği için eski JSON document'larında yoktur ve <c>null</c>
    /// deserialize olur; <see cref="EffectiveReportStatus"/> onu <c>None</c> sayar.</para>
    /// </summary>
    HealthReportStatus? ReportStatus = null,
    Guid? HealthReportAttachedById = null,
    DateTime? HealthReportAttachedAt = null,
    Guid? HealthReportReviewedById = null,
    DateTime? HealthReportReviewedAt = null,
    string? HealthReportRejectionReason = null)
{
    // SmartEnum LINQ tuzağı: Status JSON'a düz string serialize edilir; sorgular için düz string kopya.
    public string StatusName => Status.Name;

    /// <summary>Rapor durumunun null-güvenli okunuşu — kayıt yoksa <c>None</c>.</summary>
    public HealthReportStatus EffectiveReportStatus => ReportStatus ?? HealthReportStatus.None;

    /// <summary>SmartEnum LINQ tuzağı — aynı gerekçe (bkz. CLAUDE.md).</summary>
    public string ReportStatusName => EffectiveReportStatus.Name;

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

    /// <summary>
    /// Rapor eklendi (#172). Tür burada DEĞİŞMEZ — onay bekleyen bir rapor ücret kesintisini
    /// kaldırmaz. Yalnız <see cref="HealthReportAttached.RequiresApproval"/> false ise (okul
    /// tarafı doğrudan girdi, ya da #172 öncesi yazılmış eski olay) tür anında geçerli olur.
    /// </summary>
    public AttendanceRecord Apply(HealthReportAttached e) => this with
    {
        HealthReportUrl = e.ReportUrl,
        HealthReportAttachedById = e.AttachedById,
        HealthReportAttachedAt = e.AttachedAt,
        HealthReportRejectionReason = null,
        ReportStatus = e.RequiresApproval ? HealthReportStatus.Pending : HealthReportStatus.Approved,
        Type = e.RequiresApproval ? Type : AbsenceType.HealthReport
    };

    public AttendanceRecord Apply(HealthReportApproved e) => this with
    {
        ReportStatus = HealthReportStatus.Approved,
        HealthReportReviewedById = e.ApprovedById,
        HealthReportReviewedAt = e.ApprovedAt,
        Type = AbsenceType.HealthReport
    };

    /// <summary>Reddedilen rapor türü değiştirmez — kesinti hangi türdeyse öyle kalır.</summary>
    public AttendanceRecord Apply(HealthReportRejected e) => this with
    {
        ReportStatus = HealthReportStatus.Rejected,
        HealthReportReviewedById = e.RejectedById,
        HealthReportReviewedAt = e.RejectedAt,
        HealthReportRejectionReason = e.Reason
    };

    public AttendanceRecord Apply(AttendanceDeleted _) => this with
    {
        IsDeleted = true
    };
}
