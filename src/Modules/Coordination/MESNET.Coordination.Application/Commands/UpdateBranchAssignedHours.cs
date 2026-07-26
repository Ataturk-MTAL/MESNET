namespace MESNET.Coordination.Application.Commands;

/// <summary>
/// Bir alanın saat dağıtımını <b>tek işlemde</b> kaydeder (#117).
///
/// <para>Tekil <c>UpdateBusinessAssignedHours</c> korunur (elle tek satır düzenleme için);
/// bu komut yeniden dağıtım gibi <b>set</b> değiştiren akışlar içindir. Tüm satırlar tek
/// transaction'da doğrulanır ve yazılır: doğrulama geçmezse hiçbiri yazılmaz.</para>
/// </summary>
/// <param name="InstitutionId">Kurum — havuz yapılandırması bu kimlikle bulunur.</param>
/// <param name="BranchCode">Alan kodu. Boş olamaz; koordinasyon satırı alan bazlıdır (#114).</param>
/// <param name="AcademicPeriodId">Akademik dönem. Boş olamaz.</param>
/// <param name="Items">Kaydedilecek satırlar. Boş olamaz, aynı işletme iki kez geçemez.</param>
/// <param name="UpdatedBy">Değişikliği yapan kullanıcı — geçmiş kaydına yazılır.</param>
public sealed record UpdateBranchAssignedHours(
    Guid InstitutionId,
    string BranchCode,
    Guid AcademicPeriodId,
    IReadOnlyList<BranchAssignedHoursItem> Items,
    string UpdatedBy);

/// <summary>
/// Toplu kayıttaki tek satır isteği.
/// </summary>
/// <param name="IsHonoraryVisit">
/// Fahri (ücretsiz) ziyaret işareti (#115). True ise <paramref name="AssignedHours"/>
/// 0 kabul edilir; satır havuz ve öğretmen kapasitesi toplamlarına 0 katkı verir.
/// </param>
public sealed record BranchAssignedHoursItem(
    Guid BusinessId,
    int AssignedHours,
    bool IsHonoraryVisit = false);
