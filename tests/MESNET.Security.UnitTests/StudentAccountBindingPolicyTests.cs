using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Öğrenci kapsamının kullanıcı hesabına <b>hangi koşulda</b> bağlanacağı (#236).
///
/// <para><b>Yaşanan açık:</b> <c>StudentAccountSyncConsumer</c> hesabın <c>StudentId</c>'si dolu
/// ve farklıysa <b>üzerine yazıyordu</b> — yalnız eşitse atlıyordu. Hesabın kurumu ile olayın
/// kurumu da hiç karşılaştırılmıyordu. <c>UserAccount</c> kimlik katmanı belgesidir
/// (<c>DocumentTenancyMap</c> → <c>Identity</c>), kiracı damgası taşımaz; yani arama
/// <b>okullar arası globaldir</b>.</para>
///
/// <para><b>Neden kiracılık kapatmıyor:</b> conjoined kiracılık istek bağlamında süzer, bu kod
/// ise <b>olay</b> bağlamında çalışır ve damgasız bir belgeye yazar. Kapsam guard'ı da
/// kapatmaz — mesaj istekten kurum kimliği almıyor.</para>
///
/// <para><b>Nasıl tetiklenir:</b> <c>RegisterStudent</c> komutu <c>KeycloakUserId</c>'yi
/// doğrulamadan alıyor (validator yalnız <c>NotEmpty</c>). Yanlış bir kimlik, başka okulun
/// kullanıcısının hesabına o öğrencinin kapsamını bağlar. Gündemdeki toplu Excel içe aktarma
/// (#238) bunu satır sayısıyla çarpar.</para>
/// </summary>
public sealed class StudentAccountBindingPolicyTests
{
    private static readonly Guid Okul = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid BaskaOkul = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Ogrenci = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid BaskaOgrenci = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static StudentAccountBindingDecision Karar(
        Guid? hesabinOgrencisi = null, Guid? hesabinKurumu = null, Guid? olayinKurumu = null) =>
        StudentAccountBindingPolicy.Decide(
            eventStudentId: Ogrenci,
            accountStudentId: hesabinOgrencisi,
            eventInstitutionId: olayinKurumu ?? Okul,
            accountInstitutionId: hesabinKurumu ?? Okul);

    // ─── Normal yol ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Bos_hesap_baglanir()
    {
        var karar = Karar(hesabinOgrencisi: null);

        karar.ShouldBind.ShouldBeTrue();
        karar.IsWarning.ShouldBeFalse();
    }

    /// <summary>
    /// Aynı bağ yeniden gelirse iş yok ve bu <b>uyarı değildir</b> — olay yeniden işlenebilir
    /// (Wolverine retry, resync ucu). Sessiz kalması normaldir.
    /// </summary>
    [Fact]
    public void Ayni_bag_tekrar_gelirse_is_yok_ve_uyari_degil()
    {
        var karar = Karar(hesabinOgrencisi: Ogrenci);

        karar.ShouldBind.ShouldBeFalse();
        karar.IsWarning.ShouldBeFalse();
    }

    // ─── Asıl regresyon: dolu ve farklı bağ ezilmez ──────────────────────────────────

    /// <summary>
    /// <b>#236'nın kalbi.</b> Hesap zaten başka bir öğrenciye bağlıysa üzerine yazılmaz.
    /// Yazılsaydı iki yönlü zarar: yeni sahibi başkasının verisini görür, <b>gerçek sahibin
    /// bağı kopar</b> ve o kişi kendi verisini görmeyi bırakır — hata almadan, boş sonuçla.
    /// </summary>
    [Fact]
    public void Baska_ogrenciye_bagli_hesap_ezilmez()
    {
        var karar = Karar(hesabinOgrencisi: BaskaOgrenci);

        karar.ShouldBind.ShouldBeFalse();
        karar.IsWarning.ShouldBeTrue("Sessiz kalırsa çakışma hiç fark edilmez.");
    }

    // ─── Kurum çapraz kontrolü ───────────────────────────────────────────────────────

    /// <summary>
    /// Başka okulun kullanıcısına bağlanmaz. <c>UserAccount</c> damgasız olduğu için arama
    /// global; bu kontrol olmadan yanlış bir <c>KeycloakUserId</c> okul sınırını aşar.
    /// </summary>
    [Fact]
    public void Baska_okulun_hesabina_baglanmaz()
    {
        var karar = Karar(hesabinOgrencisi: null, hesabinKurumu: BaskaOkul, olayinKurumu: Okul);

        karar.ShouldBind.ShouldBeFalse();
        karar.IsWarning.ShouldBeTrue();
    }

    /// <summary>
    /// Kurum uyuşmazlığı, hesap <b>zaten bağlıyken</b> de bağlamayı açmaz — iki kontrol
    /// birbirinin yerine geçmez.
    /// </summary>
    [Fact]
    public void Kurum_uyusmazligi_dolu_hesapta_da_baglamaz()
    {
        Karar(hesabinOgrencisi: BaskaOgrenci, hesabinKurumu: BaskaOkul).ShouldBind.ShouldBeFalse();
    }

    /// <summary>
    /// <b>Bilinçli gevşeklik:</b> hesabın kurumu boşsa uyuşmazlık sayılmaz. Kapsamsız hesap
    /// "başka okula ait" kanıtı değildir ve katı davranmak meşru yolu kırardı — davetle açılan
    /// öğrenci hesabında kurum henüz dolmamış olabilir. Kalan boşluk bilinçlidir.
    /// </summary>
    [Fact]
    public void Kurumsuz_hesap_uyusmazlik_sayilmaz()
    {
        Karar(hesabinOgrencisi: null, hesabinKurumu: null).ShouldBind.ShouldBeTrue();
    }

    [Fact]
    public void Bos_guid_kurum_da_bosluk_sayilir()
    {
        Karar(hesabinOgrencisi: null, hesabinKurumu: Guid.Empty).ShouldBind.ShouldBeTrue();
    }

    /// <summary>
    /// Olayın kurumu boşsa karşılaştırılacak bir şey yoktur; kontrol <b>bağlamayı engellemez</b>
    /// ama uyuşmazlık da ilan etmez.
    /// </summary>
    [Fact]
    public void Olayin_kurumu_bossa_uyusmazlik_ilan_edilmez()
    {
        Karar(hesabinOgrencisi: null, olayinKurumu: Guid.Empty).ShouldBind.ShouldBeTrue();
    }

    // ─── Karar sırası ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Aynı bağ, kurum uyuşmazlığından <b>önce</b> değerlendirilir: yapılacak iş yoktur ve
    /// gereksiz uyarı üretmek, gerçek uyarıları gürültüye gömer.
    /// </summary>
    [Fact]
    public void Ayni_bag_kurum_uyusmazligindan_once_gelir()
    {
        var karar = Karar(hesabinOgrencisi: Ogrenci, hesabinKurumu: BaskaOkul);

        karar.ShouldBind.ShouldBeFalse();
        karar.IsWarning.ShouldBeFalse();
    }

    /// <summary>Her karar bir gerekçe taşır — log satırı gerekçesiz kalmamalı.</summary>
    [Fact]
    public void Her_karar_gerekce_tasir()
    {
        StudentAccountBindingDecision[] kararlar =
        [
            Karar(hesabinOgrencisi: null),
            Karar(hesabinOgrencisi: Ogrenci),
            Karar(hesabinOgrencisi: BaskaOgrenci),
            Karar(hesabinOgrencisi: null, hesabinKurumu: BaskaOkul),
        ];

        kararlar.ShouldAllBe(k => !string.IsNullOrWhiteSpace(k.Reason));
    }
}
