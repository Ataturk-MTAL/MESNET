namespace MESNET.Attendance.Shared.Events;

/// <summary>
/// Öğrenci ücretli izin başvurusu açtı (#177) — zincirin başlangıcı.
///
/// <para><b>Bu olay hüküm doğurmaz:</b> devamsızlık kaydı açılmaz, ücrete etki etmez. İzin
/// yalnız <see cref="PaidLeaveApproved"/> ile resmîleşir. "Giriş geniş, hüküm dar" (#172).</para>
/// </summary>
public sealed record PaidLeaveRequested(
    Guid RequestId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    DateTime StartDate,
    DateTime EndDate,
    string Reason,
    Guid RequestedById,
    DateTime RequestedAt);
