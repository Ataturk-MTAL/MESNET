namespace MESNET.Common.Shared.Reference;

/// <summary>
/// İl kodu → o ilin ilçe adları, <b>Türkçe alfabetik sırada</b>.
/// </summary>
/// <remarks>
/// <para><b>Neden ilçede ad, ilde kod:</b> il kodu (plaka) resmî, tek ve kesin bilinen bir
/// koddur — <see cref="TurkishProvinces"/> tam listeyi taşır. İlçe için elimizde aynı
/// güvenilirlikte bir kod kaynağı yok. Uydurulmuş bir ilçe kodu gerçek veri gibi görünür ve
/// yanlış kodla açılmış kaydı geriye dönük ayıklamak imkânsıza yakındır. Bu yüzden ilçe
/// **kapalı listeden seçilen ad** olarak tutulur.</para>
///
/// <para>Serbest metinden farkı: değer bu listede yoksa REDDEDİLİR. Yani
/// <c>Toroslar</c> / <c>TOROSLAR</c> / <c>toroslar </c> gibi varyantlar oluşamaz —
/// #147'nin serbest metne itirazı burada da geçerlidir, çözüm kod değil kapalı liste.</para>
///
/// <para><b>Liste eksiktir ve bu bilinçlidir.</b> Yalnız fiilen kullanılan iller doldurulur.
/// Ezberden yazılmış 973 ilçelik bir liste, eksik listeden daha kötüdür: yanlış ya da uydurma
/// ilçe adı sessizce kayda geçer. Yeni bir il devreye girdiğinde ilçeleri resmî kaynaktan
/// alınıp buraya eklenir; eklenmeden o il için ilçe girilemez (bkz. <see cref="IsKnown"/>).</para>
/// </remarks>
public static class TurkishDistricts
{
    private static readonly Dictionary<string, string[]> ByProvinceCode = new(StringComparer.Ordinal)
    {
        // 33 — Mersin (dağıtımdaki tek il). 13 ilçe.
        ["33"] =
        [
            "Akdeniz",
            "Anamur",
            "Aydıncık",
            "Bozyazı",
            "Çamlıyayla",
            "Erdemli",
            "Gülnar",
            "Mezitli",
            "Mut",
            "Silifke",
            "Tarsus",
            "Toroslar",
            "Yenişehir"
        ]
    };

    /// <summary>Bu il için ilçe listesi tanımlı mı. Değilse o ilde ilçe girilemez.</summary>
    public static bool IsKnown(string? provinceCode) =>
        provinceCode is not null && ByProvinceCode.ContainsKey(provinceCode);

    /// <summary>İlin ilçeleri, alfabetik. Liste tanımlı değilse boş dizi.</summary>
    public static IReadOnlyList<string> For(string? provinceCode) =>
        provinceCode is not null && ByProvinceCode.TryGetValue(provinceCode, out var districts)
            ? districts
            : [];

    /// <summary>
    /// İlçe adı, verilen ilin listesinde tam olarak var mı. Kırpma ya da büyük/küçük harf
    /// esnekliği YOKTUR — girdi saklanacak biçimde olmalı, yoksa aynı ilçe iki değerle kaydolur.
    /// </summary>
    public static bool IsValid(string? provinceCode, string? districtName) =>
        districtName is not null && For(provinceCode).Contains(districtName, StringComparer.Ordinal);
}
