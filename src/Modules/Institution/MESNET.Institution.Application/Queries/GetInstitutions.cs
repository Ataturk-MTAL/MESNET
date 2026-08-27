using MESNET.Common.Shared.Pagination;

namespace MESNET.Institution.Application.Queries;

/// <summary>
/// Görünür kurumların sayfalı listesi.
///
/// <para><b>Bu sorgu <c>IInstitutionScoped</c> OLAMAZ</b> — hedef kurum istekte geçmez,
/// sorulan zaten "hangi kurumlar". Kapsam bu yüzden guard'la değil <b>süzmeyle</b> uygulanır
/// (<c>InstitutionScopePolicy.VisibleScope</c>).</para>
/// </summary>
/// <param name="NodeType">
/// Düğüm tipi süzgeci — <c>Province</c> / <c>District</c> / <c>School</c>. Verilmezse
/// <b>okullar</b> döner: çağıranların ezici çoğunluğu okul listesi bekler ve varsayılan
/// süzgeçsiz olsaydı il/ilçe müdürlükleri açılır listelerde okul gibi görünürdü.
/// </param>
/// <param name="ParentId">Belirli bir düğümün doğrudan çocukları. Verilmezse tüm alt ağaç.</param>
public sealed record GetInstitutions(string? NodeType = null, Guid? ParentId = null) : PagedQuery;
