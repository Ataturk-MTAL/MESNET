namespace MESNET.Attendance.Shared.Events;

/// <summary>
/// Koordinatör öğretmen sağlık raporunu onayladı (#172) — onay zincirinin 1. adımı.
///
/// <para><b>Para etkisi bu olayla doğar:</b> devamsızlık türü <c>HealthReport</c>'a döner ve o
/// tür ücret kesintisine tabi değildir (business-rules.md §6.2). Payment modülü yalnız bu olayı
/// dinler; <see cref="HealthReportAttached"/> onun için sessizdir.</para>
///
/// <para>2. adım ayrı bir devamsızlık olayı değildir: müdür yardımcısı / müdür kesinti kararını
/// mevcut dekont onay zincirinde (<c>salary:approve</c>) uygular.</para>
/// </summary>
public sealed record HealthReportApproved(
    Guid AttendanceId,
    Guid StudentId,
    Guid ApprovedById,
    DateTime ApprovedAt);
