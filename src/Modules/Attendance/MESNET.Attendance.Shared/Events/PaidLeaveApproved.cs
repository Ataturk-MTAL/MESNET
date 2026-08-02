namespace MESNET.Attendance.Shared.Events;

/// <summary>
/// Okul ücretli izin başvurusunu onayladı (#177) — izin RESMÎDİR.
///
/// <para><b>Para etkisi bu olayla doğar:</b> tarih aralığındaki çalışma günleri için
/// <c>PaidLeave</c> türünde devamsızlık kaydı açılır ve o tür ücret kesintisine tabi değildir
/// (business-rules.md §6.2). Zincirin önceki iki olayı bu sonucu doğurmaz.</para>
///
/// <para>Tarih aralığı ve kapsam alanları olayda TAŞINIR: kayıtları açan tüketici aggregate'i
/// yeniden yüklemek zorunda kalmasın diye (event-carried state transfer).</para>
/// </summary>
public sealed record PaidLeaveApproved(
    Guid RequestId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    DateTime StartDate,
    DateTime EndDate,
    string Reason,
    Guid ApprovedById,
    DateTime ApprovedAt);
