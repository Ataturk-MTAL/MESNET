using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Shared.Events;

namespace MESNET.Attendance.Core.Aggregates;

/// <summary>
/// MESEM ücretli izin başvurusu (#177) — öğrenci açar, işletme ve okul onaylar.
///
/// <para><b>Neden ayrı varlık:</b> ücretli izin önceden bilinir (telafi eğitimi, okulda sınav,
/// tatil izni — MEB Ortaöğretim Kurumları Yönetmeliği (ı) ve (i)). Devamsızlık girişi "yalnız bu
/// hafta" kısıtı taşır (e-Okul uyumu), ileri tarihli izin o kısıta takılırdı. "Önce mazeretsiz
/// yaz, sonra düzelt" akışı ise yanlış veriyi bir süre canlı tutar ve o sürede ücret kesintisi
/// doğurur.</para>
///
/// <para>Durum geçişi olan varlık → event sourcing (<c>AttendanceRecord</c> ile aynı çizgi).</para>
/// </summary>
public sealed record PaidLeaveRequest(
    Guid Id,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    DateTime StartDate,
    DateTime EndDate,
    string Reason,
    PaidLeaveStatus Status,
    Guid RequestedById,
    DateTime RequestedAt,
    Guid? BusinessApprovedById = null,
    DateTime? BusinessApprovedAt = null,
    Guid? ApprovedById = null,
    DateTime? ApprovedAt = null,
    Guid? RejectedById = null,
    DateTime? RejectedAt = null,
    string? RejectionReason = null)
{
    /// <summary>SmartEnum LINQ tuzağı — düz string kopya (bkz. CLAUDE.md).</summary>
    public string StatusName => Status.Name;

    public static PaidLeaveRequest Create(PaidLeaveRequested e) => new(
        e.RequestId,
        e.StudentId,
        e.BusinessId,
        e.InstitutionId,
        e.AcademicPeriodId,
        e.StartDate,
        e.EndDate,
        e.Reason,
        PaidLeaveStatus.PendingBusiness,
        e.RequestedById,
        e.RequestedAt);

    public PaidLeaveRequest Apply(PaidLeaveBusinessApproved e) => this with
    {
        Status = PaidLeaveStatus.PendingSchool,
        BusinessApprovedById = e.ApprovedById,
        BusinessApprovedAt = e.ApprovedAt
    };

    public PaidLeaveRequest Apply(PaidLeaveApproved e) => this with
    {
        Status = PaidLeaveStatus.Approved,
        ApprovedById = e.ApprovedById,
        ApprovedAt = e.ApprovedAt
    };

    public PaidLeaveRequest Apply(PaidLeaveRejected e) => this with
    {
        Status = PaidLeaveStatus.Rejected,
        RejectedById = e.RejectedById,
        RejectedAt = e.RejectedAt,
        RejectionReason = e.Reason
    };
}
