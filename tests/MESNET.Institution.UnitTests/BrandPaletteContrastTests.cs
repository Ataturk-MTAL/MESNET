using MESNET.Institution.Core.Enums;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// <b>Küratörlü marka paletinin kontrast kilidi.</b>
///
/// <para><b>Neden kilit gerekiyor:</b> kiracı (okul) primary/secondary'yi kaydırabiliyor, ama
/// arayüzdeki metin renkleri kaymıyor — üst bardaki, birincil/ikincil butondaki ve rozetlerdeki
/// metin beyazdır, yumuşak zeminlerdeki metin primary'den türetilir. Yani her yeni palet
/// seçeneği, ölçülmemişse, <b>okunmaz metin</b> demektir ve bunu ne derleyici ne de bir arayüz
/// testi görür: renk yanlış değil, yalnız zayıftır. Bu test palete yeni renk eklendiğinde
/// kırmızıya döner — amacı budur.</para>
///
/// <para><b>Anlamsal renkler burada ölçülmez</b>, çünkü kiracıya göre değişmezler:
/// positive / negative / info / warning ve Resmî Hardal (accent) sabittir
/// (<c>src/WebUI/src/assets/quasar-variables.sass</c>). Onların kontrastı bir kez ölçüldü ve
/// palet seçiminden bağımsız olarak geçerli kalır — bu ayrım kasıtlıdır.</para>
///
/// <para><b>Kapılar nereden geliyor:</b> altı kapının beşi <c>app.css</c>'te fiilen kullanılan
/// türetilmiş yüzeydir (yumuşak çip 12%/88%, yumuşak zemin 8%, soluk yüzey 55% + #78808c).
/// Uydurulmuş kombinasyon değildir; arayüz o karışımları gerçekten boyar.</para>
///
/// <para><b>Ölçüm — WCAG 2.x, sRGB göreli parlaklık:</b></para>
/// <code>
/// // Kanal doğrusallaştırma (sRGB → lineer), c 0..1 aralığında:
/// //   c &lt;= 0,03928  →  c / 12,92
/// //   c &gt;  0,03928  →  ((c + 0,055) / 1,055) ^ 2,4
/// //
/// // Göreli parlaklık:
/// //   L = 0,2126 * R + 0,7152 * G + 0,0722 * B
/// //
/// // Kontrast oranı (L1 = açık olan, L2 = koyu olan):
/// //   CR = (L1 + 0,05) / (L2 + 0,05)
/// //
/// // color-mix(in srgb, A p%, B) — sRGB (gama) uzayında düz karışım, 0..255 üzerinden:
/// //   sonuç = A * p + B * (1 - p)
/// </code>
///
/// <para><b>Ölçülen en dar kapı:</b> türetilmiş ikincil butonun beyaz metni — <c>Orman</c>
/// (#467B5B) 4,95:1, eşik 4,5:1. Palete yeni seçenek eklenirse önce O kapı ölçülmelidir.</para>
/// </summary>
public sealed class BrandPaletteContrastTests
{
    /// <summary>WCAG AA — normal boy metin.</summary>
    private const double TextThreshold = 4.5;

    /// <summary>WCAG AA — arayüz bileşeni / grafik nesnesi sınırı.</summary>
    private const double GraphicThreshold = 3.0;

    private static readonly Rgb White = Rgb.FromHex("#FFFFFF");
    private static readonly Rgb Black = Rgb.FromHex("#000000");

    /// <summary>
    /// Soluk yüzeyin karıştırıldığı nötr gri — <c>app.css</c>'teki
    /// <c>color-mix(in srgb, var(--q-primary) 55%, #78808c)</c> ile birebir aynı.
    /// </summary>
    private static readonly Rgb MutedNeutral = Rgb.FromHex("#78808C");

    public static TheoryData<string> AllPalettes()
    {
        var data = new TheoryData<string>();
        foreach (var palette in InstitutionBrandPalette.List)
            data.Add(palette.Name);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllPalettes))]
    public void Birincil_yuzeyde_beyaz_metin_okunur(string paletteName)
    {
        var primary = Rgb.FromHex(InstitutionBrandPalette.FromName(paletteName).Primary);

        Contrast(primary, White).ShouldBeGreaterThanOrEqualTo(TextThreshold,
            $"{paletteName}: üst bar ve birincil butonun beyaz metni okunmuyor.");
    }

    [Theory]
    [MemberData(nameof(AllPalettes))]
    public void Birincil_renk_beyaz_zeminde_grafik_olarak_secilir(string paletteName)
    {
        var primary = Rgb.FromHex(InstitutionBrandPalette.FromName(paletteName).Primary);

        Contrast(primary, White).ShouldBeGreaterThanOrEqualTo(GraphicThreshold,
            $"{paletteName}: beyaz kart üzerindeki çizgi/ikon/grafik sınırı seçilmiyor.");
    }

    [Theory]
    [MemberData(nameof(AllPalettes))]
    public void Ikincil_yuzeyde_beyaz_metin_okunur(string paletteName)
    {
        var secondary = Rgb.FromHex(InstitutionBrandPalette.FromName(paletteName).Secondary);

        // Bağlayıcı kapı: Secondary elle seçilmez, Primary'den OKLCH kuralıyla türetilir —
        // yani palete eklenen her yeni renk burada en dar sonucu verir.
        Contrast(secondary, White).ShouldBeGreaterThanOrEqualTo(TextThreshold,
            $"{paletteName}: türetilmiş ikincil butonun beyaz metni okunmuyor.");
    }

    [Theory]
    [MemberData(nameof(AllPalettes))]
    public void Yumusak_cipte_koyu_metin_okunur(string paletteName)
    {
        var primary = Rgb.FromHex(InstitutionBrandPalette.FromName(paletteName).Primary);

        // app.css: bg = color-mix(in srgb, var(--q-primary) 12%, #fff)
        //          fg = color-mix(in srgb, var(--q-primary) 88%, #000)
        var background = Mix(primary, 0.12, White);
        var foreground = Mix(primary, 0.88, Black);

        Contrast(foreground, background).ShouldBeGreaterThanOrEqualTo(TextThreshold,
            $"{paletteName}: yumuşak tonlu rozet/çip metni okunmuyor.");
    }

    [Theory]
    [MemberData(nameof(AllPalettes))]
    public void Yumusak_zeminde_birincil_metin_okunur(string paletteName)
    {
        var primary = Rgb.FromHex(InstitutionBrandPalette.FromName(paletteName).Primary);

        // app.css: bg = color-mix(in srgb, var(--q-primary) 8%, #fff), metin düz primary
        var background = Mix(primary, 0.08, White);

        Contrast(primary, background).ShouldBeGreaterThanOrEqualTo(TextThreshold,
            $"{paletteName}: yumuşak zemin üzerindeki birincil metin okunmuyor.");
    }

    [Theory]
    [MemberData(nameof(AllPalettes))]
    public void Soluk_yuzeyde_beyaz_metin_okunur(string paletteName)
    {
        var primary = Rgb.FromHex(InstitutionBrandPalette.FromName(paletteName).Primary);

        // app.css: color-mix(in srgb, var(--q-primary) 55%, #78808c)
        var muted = Mix(primary, 0.55, MutedNeutral);

        Contrast(muted, White).ShouldBeGreaterThanOrEqualTo(TextThreshold,
            $"{paletteName}: soluk/pasif rozetin beyaz metni okunmuyor.");
    }

    /// <summary>
    /// Palet <b>anahtarla</b> saklandığı için hex'lerin biçimi de kilitlenir: kayda düşen değer
    /// anahtardır, hex yalnız koddan gelir — bozuk bir hex buraya sızarsa kontrast ölçümü
    /// sessizce anlamsızlaşırdı.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllPalettes))]
    public void Hex_degerleri_alti_haneli_ve_gecerlidir(string paletteName)
    {
        var palette = InstitutionBrandPalette.FromName(paletteName);

        foreach (var hex in new[] { palette.Primary, palette.Secondary })
        {
            hex.Length.ShouldBe(7, $"{paletteName}: hex 7 karakter olmalı (#RRGGBB), gelen: {hex}");
            hex[0].ShouldBe('#');
            Should.NotThrow(() => Rgb.FromHex(hex), $"{paletteName}: geçersiz hex {hex}");
        }
    }

    /// <summary>Varsayılan palet kümenin içindedir ve tektir.</summary>
    [Fact]
    public void Varsayilan_palet_kumeye_aittir()
    {
        InstitutionBrandPalette.List.ShouldContain(InstitutionBrandPalette.Default);
        InstitutionBrandPalette.Default.Name.ShouldBe(nameof(InstitutionBrandPalette.Lacivert));
    }

    /// <summary>
    /// Null ve tanınmayan anahtar aynı yere — varsayılana — düşer. Bu, "veriye düşen bozuk bir
    /// değer kontrastı kıramaz" güvencesinin çalıştığı tek nokta.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("neon-pembe")]
    [InlineData("#FF00FF")]
    public void Taninmayan_anahtar_varsayilana_duser(string? stored)
    {
        InstitutionBrandPalette.Resolve(stored).ShouldBe(InstitutionBrandPalette.Default);
    }

    /// <summary>Anahtar büyük/küçük harf farkıyla da çözülür; kanonik ad geri döner.</summary>
    [Fact]
    public void Anahtar_buyuk_kucuk_harf_duyarsiz_cozulur()
    {
        InstitutionBrandPalette.Resolve("bordo").Name.ShouldBe(nameof(InstitutionBrandPalette.Bordo));
    }

    // ─── WCAG 2.x ölçüm yardımcıları ────────────────────────────────────────────────────

    private readonly record struct Rgb(double R, double G, double B)
    {
        public static Rgb FromHex(string hex)
        {
            var value = hex.TrimStart('#');
            return new Rgb(
                Convert.ToInt32(value.Substring(0, 2), 16),
                Convert.ToInt32(value.Substring(2, 2), 16),
                Convert.ToInt32(value.Substring(4, 2), 16));
        }
    }

    /// <summary>
    /// <c>color-mix(in srgb, <paramref name="first"/> p%, <paramref name="second"/>)</c> —
    /// CSS bunu sRGB (gama) uzayında düz doğrusal karışım olarak hesaplar, lineer ışıkta değil.
    /// Ölçümün tarayıcının boyadığı renkle örtüşmesi için karışım burada da gama uzayındadır.
    /// </summary>
    private static Rgb Mix(Rgb first, double ratio, Rgb second) => new(
        first.R * ratio + second.R * (1 - ratio),
        first.G * ratio + second.G * (1 - ratio),
        first.B * ratio + second.B * (1 - ratio));

    /// <summary>WCAG 2.x göreli parlaklık: L = 0,2126·R + 0,7152·G + 0,0722·B (lineerleştirilmiş).</summary>
    private static double RelativeLuminance(Rgb color) =>
        0.2126 * Linearize(color.R) + 0.7152 * Linearize(color.G) + 0.0722 * Linearize(color.B);

    private static double Linearize(double channel)
    {
        var c = channel / 255.0;
        return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
    }

    /// <summary>CR = (L_açık + 0,05) / (L_koyu + 0,05).</summary>
    private static double Contrast(Rgb a, Rgb b)
    {
        var la = RelativeLuminance(a);
        var lb = RelativeLuminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }
}
