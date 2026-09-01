using MESNET.Common.Shared.Security;
using MESNET.Institution.Core.Enums;
using InstitutionRecord = MESNET.Institution.Core.Entities.Institution;

namespace MESNET.Institution.Application.Extensions;

/// <summary>
/// Kurum ağacı sorgularının paylaştığı süzgeçler — <b>tek yer</b>.
///
/// <para><b>Neden uzantı, neden elle <c>Where</c> değil:</b> <c>Institution</c> artık "okul"
/// demek değil. Okul listesi üreten her sorgu tipe göre süzmek zorundadır ve süzmeyen sorgu
/// il/ilçe müdürlüğünü <b>okul sanar</b> — bu sessizce olur: açılır listede bir MEM adı belirir,
/// kimse hata görmez. Kuralı tek fonksiyona bağlamak
/// <c>InstitutionNodeTypeDriftTests</c>'in taranabilir bir hedefi olmasını sağlar. Aynı gerekçe
/// kapsam süzgeci (<see cref="ApplyScope"/>) için de geçerlidir: birden çok sorgu aynı kararı
/// (<see cref="InstitutionVisibility"/>) Marten sorgusuna çeviriyorsa, çeviri tek fonksiyonda
/// olmalı — yoksa biri düzeltilip diğeri sessizce eskide kalır.</para>
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

    /// <summary>
    /// Kapsam daraltması. Üç hâl vardır ve üçü de <see cref="InstitutionVisibility"/>'den gelir;
    /// karar burada TEKRARLANMAZ — bu fonksiyon yalnız kararı Marten sorgusuna çevirir.
    ///
    /// <para><b>Neden tek fonksiyon:</b> <c>GetInstitutionsHandler</c> ve
    /// <c>GetUnmanagedInstitutionsHandler</c> aynı üç hâli aynı biçimde süzer. Kopya iki dosyada
    /// yaşasaydı birine yapılan bir düzeltme diğerine sessizce yansımazdı.</para>
    /// </summary>
    public static IQueryable<InstitutionRecord> ApplyScope(
        this IQueryable<InstitutionRecord> queryable, InstitutionVisibility scope)
    {
        if (scope.Unrestricted)
            return queryable;

        // Boş/whitespace önek asla kapsamı GENİŞLETMEMELİDİR: boş dize StartsWith("")
        // her satırla eşleşir ve Marten'de LIKE '%' üretir — kapsamsız aktörü tüm ağacı gören
        // aktöre çevirir. SubtreeTenantScope.ResolveAsync aynı denetimi yapar; ikisi arasında
        // fark bırakmak yalnız InstitutionScopePolicy.VisibleScope'un bugün asla boş dize
        // üretmemesine bağlıydı — güvenli olmayan bir varsayım. Boş/whitespace önek burada da
        // kimliğe düşen dala düşer (aşağıya bakınız), tıpkı önek hiç yokmuş gibi.
        if (scope.PathPrefix is { } prefix && !string.IsNullOrWhiteSpace(prefix))
        {
            // Marten string.StartsWith'i SQL'de LIKE 'önek%' çevirir; ham SQL ve
            // WITH RECURSIVE gerekmez. Yolu olmayan satır alt ağaçta DEĞİLDİR.
            return queryable.Where(i => i.Path != null && i.Path.StartsWith(prefix));
        }

        // Yol yok: kimliğe düş. Kapsamsız aktörde bu Guid.Empty'dir ve hiçbir kurumla
        // eşleşmez — her şeyi görmek yerine hiçbir şey görmek.
        var institutionId = scope.InstitutionId ?? Guid.Empty;
        return queryable.Where(i => i.Id == institutionId);
    }
}
