using MESNET.Institution.Core.Enums;
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.Application.Extensions;

/// <summary>
/// Kurum ağacında düğüm tipine göre süzme — <b>tek yer</b>.
///
/// <para><b>Neden uzantı, neden elle <c>Where</c> değil:</b> <c>Institution</c> artık "okul"
/// demek değil. Okul listesi üreten her sorgu tipe göre süzmek zorundadır ve süzmeyen sorgu
/// il/ilçe müdürlüğünü <b>okul sanar</b> — bu sessizce olur: açılır listede bir MEM adı belirir,
/// kimse hata görmez. Kuralı tek fonksiyona bağlamak
/// <c>InstitutionNodeTypeDriftTests</c>'in taranabilir bir hedefi olmasını sağlar.</para>
/// </summary>
public static class InstitutionQueryExtensions
{
    /// <summary>
    /// Verilen düğüm tipine daraltır.
    ///
    /// <para><b>Okul sorgusu boş <c>NodeTypeName</c>'i de kapsar</b> — geçiş ucu koşturulmamış
    /// kayıtların hepsi okuldur. Kapsamasaydı okul listesi dağıtımdan sonra boş gelirdi: hata
    /// değil, sessiz boşluk.</para>
    ///
    /// <para>Karşılaştırma düz <c>NodeTypeName</c> alanına yapılır. SmartEnum özelliği
    /// (<c>i.NodeType.Name</c>) Marten'de <c>data->'nodeType'->>'Name'</c> üretir ve HER ZAMAN
    /// NULL döner.</para>
    /// </summary>
    public static IQueryable<InstitutionRecord> OfNodeType(
        this IQueryable<InstitutionRecord> queryable, InstitutionNodeType nodeType)
    {
        var name = nodeType.Name;

        if (nodeType == InstitutionNodeType.School)
            return queryable.Where(i => i.NodeTypeName == null || i.NodeTypeName == name);

        return queryable.Where(i => i.NodeTypeName == name);
    }
}
