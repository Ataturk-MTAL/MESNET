using Ardalis.SmartEnum;

namespace MESNET.Institution.Core.Enums;

/// <summary>
/// Kiracının (okulun) seçebileceği <b>küratörlü</b> marka paleti.
///
/// <para><b>Neden serbest renk değil:</b> sistemin bütün kontrast ölçümleri primary'ye
/// bağlıdır. Üst bardaki, birincil butondaki ve rozetlerdeki metin beyazdır; serbest hex
/// girişi o metni okunmaz yapar ve bunu hiçbir derleyici göremez. Sekiz seçeneğin her biri
/// on gerçek yüzeyde ölçüldü — en dar kapı türetilmiş ikincil butonun beyaz metnidir
/// (<c>Orman</c>, 4,95:1; eşik 4,5:1). Kilitleyen test:
/// <c>tests/MESNET.Institution.UnitTests/BrandPaletteContrastTests.cs</c>.</para>
///
/// <para><b>Veritabanında ham hex SAKLANMAZ</b>, bu kümeye bir <b>anahtar</b> saklanır
/// (<see cref="Ardalis.SmartEnum.SmartEnum{TEnum,TValue}.Name"/>). Palet kodda yaşar: veriye
/// düşen bozuk bir değer kontrastı kıramaz, yalnız <see cref="Resolve"/> ile varsayılana
/// düşer. Hex'i buradan başka bir yerde ikinci kez tanımlamayın — arayüz de katalog ucundan
/// (<c>GET /api/institutions/brand-palettes</c>) okur.</para>
///
/// <para><b>Anlamsal renkler kiracıya göre DEĞİŞMEZ.</b> positive / negative / info /
/// warning ve Resmî Hardal (accent) sabittir; bunlar marka ifadesi değil sistem anlamıdır.
/// Yalnız primary ve secondary kayar — ölçülmüş anlamsal kontrastlar böylece her kiracıda
/// geçerli kalır.</para>
///
/// <para><b>Palete yeni seçenek eklenirse</b> önce en dar kapı ölçülmelidir: türetilmiş
/// ikincil butonun beyaz metni. <c>Secondary</c> mevcut sekizde <c>Primary</c>'den OKLCH
/// kuralıyla türetildi (L +19,2pp, kroma ×1,28, hue sabit).</para>
/// </summary>
public sealed class InstitutionBrandPalette : SmartEnum<InstitutionBrandPalette>
{
    public static readonly InstitutionBrandPalette Lacivert =
        new(nameof(Lacivert), 1, "Mührü Lacivert", "#1E3A5F", "#4870A4");

    public static readonly InstitutionBrandPalette Deniz =
        new(nameof(Deniz), 2, "Deniz Mavisi", "#123A63", "#3A70AA");

    public static readonly InstitutionBrandPalette Petrol =
        new(nameof(Petrol), 3, "Petrol Yeşili", "#0E4146", "#387980");

    public static readonly InstitutionBrandPalette Orman =
        new(nameof(Orman), 4, "Orman Yeşili", "#1B422C", "#467B5B");

    public static readonly InstitutionBrandPalette Bordo =
        new(nameof(Bordo), 5, "Bordo", "#6B1F2E", "#B54B5C");

    public static readonly InstitutionBrandPalette Erguvan =
        new(nameof(Erguvan), 6, "Erguvan", "#4A2352", "#885193");

    public static readonly InstitutionBrandPalette Indigo =
        new(nameof(Indigo), 7, "İndigo", "#2A3072", "#5763C0");

    public static readonly InstitutionBrandPalette Antrasit =
        new(nameof(Antrasit), 8, "Antrasit", "#2B3138", "#5C656F");

    /// <summary>Türkçe görünen ad — arayüz etiketi ve hata mesajı için.</summary>
    public string Slug { get; }

    /// <summary>Birincil marka rengi (<c>$primary</c> / <c>--q-primary</c>).</summary>
    public string Primary { get; }

    /// <summary>
    /// İkincil marka rengi (<c>$secondary</c> / <c>--q-secondary</c>) — <see cref="Primary"/>'den
    /// OKLCH kuralıyla türetildi, elle seçilmedi.
    /// </summary>
    public string Secondary { get; }

    private InstitutionBrandPalette(string name, int value, string slug, string primary, string secondary)
        : base(name, value)
    {
        Slug = slug;
        Primary = primary;
        Secondary = secondary;
    }

    /// <summary>
    /// Seçim yapılmamış kurumun paleti. <c>Institution.BrandPaletteName</c> null ise bu geçerlidir
    /// — mevcut kurumların görünümü değişmez, çünkü bugünkü tema zaten bu renklerdir.
    /// </summary>
    public static InstitutionBrandPalette Default => Lacivert;

    /// <summary>
    /// Saklanan anahtarı palete çevirir; <b>null ya da tanınmayan</b> değer
    /// <see cref="Default"/>'a düşer.
    ///
    /// <para>Fallback burada tek yerde durur ve kasıtlıdır: palete ileride bir seçenek
    /// eklenip çıkarılırsa o anahtarı taşıyan eski kayıtlar okunmaya devam eder. Kural 3'ün
    /// (<c>ProvinceCode</c> deseni) renk tarafındaki karşılığıdır — eksik/eskimiş alan kaydı
    /// okunamaz hâle getirmez.</para>
    /// </summary>
    public static InstitutionBrandPalette Resolve(string? name) =>
        name is not null && TryFromName(name, ignoreCase: true, out var palette)
            ? palette
            : Default;
}
