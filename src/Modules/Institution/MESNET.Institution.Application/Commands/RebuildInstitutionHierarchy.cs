namespace MESNET.Institution.Application.Commands;

/// <summary>
/// Kurum ağacını mevcut okul künyelerinden yeniden kurar. <b>İdempotent</b>: ikinci koşu aynı
/// ağacı üretir, düğüm çoğaltmaz.
///
/// <para>Kurum kimliği <b>taşımaz</b> — kurum üstü bir iştir ve kapsamı izniyle sınırlıdır
/// (<c>platform:tenant:manage</c>). <c>IInstitutionScoped</c> uygulanamaz: karşılaştırılacak
/// tek bir hedef yoktur.</para>
/// </summary>
public sealed record RebuildInstitutionHierarchy;

/// <param name="ProvincesCreated">Yeni kurulan il müdürlüğü düğümü sayısı.</param>
/// <param name="DistrictsCreated">Yeni kurulan ilçe müdürlüğü düğümü sayısı.</param>
/// <param name="NodesUpdated">Ağaç alanları yazılan düğüm sayısı (yeniler dahil).</param>
/// <param name="SkippedNoProvince">
/// İl kodu olmadığı için <b>kapsamsız</b> bırakılan okul sayısı. Sıfırdan büyükse o okullar
/// hiçbir il yetkilisinin listesinde görünmez — künyeleri tamamlanıp uç yeniden çağrılmalıdır.
/// </param>
public sealed record RebuildInstitutionHierarchyResult(
    int ProvincesCreated,
    int DistrictsCreated,
    int NodesUpdated,
    int SkippedNoProvince);
