namespace MESNET.Coordination.Application.Queries;

/// <summary>
/// Bir alanın ders yükü havuzunu işletmelerine dağıtan <b>öneri</b> sorgusu (issue #116).
///
/// <para><b>Hiçbir şey yazmaz.</b> Sonuç yalnız öneri + tanılamadır; kaydetme ayrı ve
/// atomik adımdır (<c>UpdateBranchAssignedHours</c>, #117) — insan onayı zincirde kalır.</para>
/// </summary>
/// <param name="InstitutionId">Kurum — havuz ve koordinatörlük yapılandırması bu kimlikle bulunur.</param>
/// <param name="BranchCode">Alan kodu. Boş olamaz; planlama alan bazlıdır (#114).</param>
/// <param name="AcademicPeriodId">Akademik dönem. Boş olamaz.</param>
/// <param name="Semester">
/// Yarıyıl (<c>Fall</c> / <c>Spring</c> / <c>Summer</c>) — öğretmen kapasitesi (<c>C</c>)
/// o yarıyılın ders programındaki boş slotlardan hesaplanır.
/// </param>
/// <param name="Pinned">
/// Koordinatörün kilitlediği satırlar, <c>"işletmeKimliği:saat,..."</c> biçiminde
/// (bkz. <c>PinnedHoursSelection</c>). Null/boş → kilitli satır yok.
/// </param>
public sealed record SuggestAssignedHours(
    Guid InstitutionId,
    string BranchCode,
    Guid AcademicPeriodId,
    string Semester,
    string? Pinned = null);
