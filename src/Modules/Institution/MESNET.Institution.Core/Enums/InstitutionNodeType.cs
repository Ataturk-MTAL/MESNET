using Ardalis.SmartEnum;

namespace MESNET.Institution.Core.Enums;

/// <summary>
/// Kurum ağacındaki düğümün tipi. İl müdürlüğü, ilçe müdürlüğü ve okul aynı belge tipinin
/// farklı tipleridir — kullanıcı–kurum bağı tek kural olarak kalsın diye (herkes bir kuruma
/// bağlanır, tipi ne olursa olsun).
///
/// <para><b>Bugün üretilen tip sayısı üçtür.</b> Ağacın sonsuz derinliği bedava bir yan
/// üründür, hedeflenen özellik değil: modellenen seviye il ve ilçedir (30.07.2026 kapsam
/// kararı — Bakanlık düzeyi aktör ve iller arası federasyon tasarlanmaz).</para>
/// </summary>
public sealed class InstitutionNodeType : SmartEnum<InstitutionNodeType>
{
    public static readonly InstitutionNodeType Province =
        new(nameof(Province), 1, "İl Millî Eğitim Müdürlüğü");

    public static readonly InstitutionNodeType District =
        new(nameof(District), 2, "İlçe Millî Eğitim Müdürlüğü");

    public static readonly InstitutionNodeType School =
        new(nameof(School), 3, "Okul");

    /// <summary>Türkçe arayüz etiketi. <see cref="SmartEnum{T}.Name"/> İngilizcedir ve serialize edilir.</summary>
    public string Slug { get; }

    private InstitutionNodeType(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    /// <summary>
    /// Saklanan adı düğüm tipine çevirir. Boş ve tanınmayan değer <see cref="School"/>'a düşer.
    ///
    /// <para><b>Boş neden okul:</b> mevcut kurum kayıtları bu alan olmadan saklandı ve hepsi
    /// okuldur. Başka bir şeye düşseydi, geçiş ucu koşturulana kadar okul listesi boş gelirdi —
    /// hata değil, sessiz boşluk.</para>
    ///
    /// <para><b>Tanınmayan da okul:</b> en dar okuma. <see cref="Province"/> sayılsaydı bozuk
    /// tek bir satır kendine bir alt ağaç uydururdu. Aynı gerekçe
    /// <c>InstitutionBrandPalette.Resolve</c> içinde de var.</para>
    /// </summary>
    public static InstitutionNodeType Resolve(string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && TryFromName(name.Trim(), ignoreCase: true, out var found)
            ? found
            : School;
}
