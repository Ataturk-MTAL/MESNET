using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// <c>UserAccount</c> kaydının Keycloak'tan sapmasını tespit eden karar mantığı (#208).
///
/// <para><b>Neden gerekli:</b> <c>PermissionClaimsTransformation</c> izinleri kayıttan türetir
/// ve <b>kayıt varsa token'daki rollere hiç bakmaz</b> — kayıt otoriterdir. Bu bilinçli bir
/// karar, ama bedeli şu: Keycloak'ta yapılan rol değişikliği senkronizasyon çağrılmadan sisteme
/// hiç ulaşmaz ve <b>bunu gören hiçbir kontrol yoktu</b>.</para>
///
/// <para>Gerçekten yaşandı (#205): <c>admin</c>'e Keycloak'ta <c>SystemAdmin</c> atandı, token
/// da onu taşıyordu, ama kayıt hâlâ <c>["InstitutionManager"]</c> diyordu ve
/// <c>PUT /api/payments/config/minimum-wage</c> 403 dönmeye devam etti. Hiçbir yerde uyarı
/// yoktu; düzeltmenin neden işe yaramadığı ancak veritabanı elle okunarak anlaşıldı.</para>
///
/// <para><b>Zaman koşulu neden var:</b> yalnız "token'da var, kayıtta yok" bakılsaydı, uygulama
/// üzerinden yapılan her <b>rol kaldırma</b> işlemi token ömrü boyunca (realm'de 1800 sn) yanlış
/// alarm üretirdi — kayıt güncel, token eski. Gürültü çıkaran kontrol kısa sürede görmezden
/// gelinir; o zaman gerçek sapma da kaçar.</para>
/// </summary>
public sealed class UserAccountDriftPolicyTests
{
    private static readonly DateTime KayitZamani = new(2026, 8, 6, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SonrakiToken = KayitZamani.AddMinutes(5);
    private static readonly DateTime OncekiToken = KayitZamani.AddMinutes(-5);

    /// <summary>#205'te canlıda yaşanan durum.</summary>
    [Fact]
    public void Token_kayitta_olmayan_rol_tasiyorsa_sapma_bildirilir()
    {
        var eksik = UserAccountDriftPolicy.Detect(
            recordRoles: [MesnetRoles.InstitutionManager],
            recordWrittenAt: KayitZamani,
            tokenRoles: [MesnetRoles.InstitutionManager, MesnetRoles.SystemAdmin],
            tokenIssuedAt: SonrakiToken);

        eksik.ShouldBe([MesnetRoles.SystemAdmin]);
    }

    [Fact]
    public void Birden_cok_eksik_rol_tamami_bildirilir()
    {
        var eksik = UserAccountDriftPolicy.Detect(
            recordRoles: [],
            recordWrittenAt: KayitZamani,
            tokenRoles: [MesnetRoles.Teacher, MesnetRoles.DepartmentHead],
            tokenIssuedAt: SonrakiToken);

        eksik.ShouldBe([MesnetRoles.DepartmentHead, MesnetRoles.Teacher], ignoreOrder: true);
    }

    // ── Yanlış alarm üretmemesi gereken durumlar ───────────────────────────────────────

    /// <summary>
    /// <b>Asıl gürültü kaynağı.</b> Uygulamadan rol kaldırıldığında kayıt anında güncellenir,
    /// ama kullanıcının elindeki token o rolü taşımaya devam eder (realm'de 1800 sn). Kayıt
    /// token'dan yeniyse divergence sapma değil, <b>beklenen</b> durumdur.
    /// </summary>
    [Fact]
    public void Kayit_tokendan_yeniyse_sapma_sayilmaz()
    {
        var eksik = UserAccountDriftPolicy.Detect(
            recordRoles: [MesnetRoles.InstitutionManager],
            recordWrittenAt: KayitZamani,
            tokenRoles: [MesnetRoles.InstitutionManager, MesnetRoles.SystemAdmin],
            tokenIssuedAt: OncekiToken);

        eksik.ShouldBeEmpty("Rol uygulamadan kaldırılmış olabilir; eski token sapma değildir.");
    }

    /// <summary>Aynı anda yazılmış kayıt da sapma sayılmaz — sıra belirsizken suçlama yapılmaz.</summary>
    [Fact]
    public void Esit_zaman_damgasi_sapma_sayilmaz()
    {
        UserAccountDriftPolicy.Detect(
            recordRoles: [],
            recordWrittenAt: KayitZamani,
            tokenRoles: [MesnetRoles.SystemAdmin],
            tokenIssuedAt: KayitZamani).ShouldBeEmpty();
    }

    /// <summary>
    /// Kayıtta fazladan rol bulunması sapma DEĞİLDİR — kayıt otoriter olduğu için orada
    /// bulunan rol zaten yürürlüktedir. Denetlenen tek yön "token biliyor, kayıt bilmiyor".
    /// </summary>
    [Fact]
    public void Kayitta_fazladan_rol_sapma_sayilmaz()
    {
        UserAccountDriftPolicy.Detect(
            recordRoles: [MesnetRoles.InstitutionManager, MesnetRoles.SystemAdmin],
            recordWrittenAt: KayitZamani,
            tokenRoles: [MesnetRoles.InstitutionManager],
            tokenIssuedAt: SonrakiToken).ShouldBeEmpty();
    }

    [Fact]
    public void Ayni_roller_sapma_uretmez()
    {
        UserAccountDriftPolicy.Detect(
            recordRoles: [MesnetRoles.Teacher],
            recordWrittenAt: KayitZamani,
            tokenRoles: [MesnetRoles.Teacher],
            tokenIssuedAt: SonrakiToken).ShouldBeEmpty();
    }

    [Fact]
    public void Rol_adi_buyuk_kucuk_harfe_duyarsiz_eslesir()
    {
        UserAccountDriftPolicy.Detect(
            recordRoles: [MesnetRoles.Teacher.ToUpperInvariant()],
            recordWrittenAt: KayitZamani,
            tokenRoles: [MesnetRoles.Teacher],
            tokenIssuedAt: SonrakiToken).ShouldBeEmpty();
    }

    /// <summary>
    /// Kaydın yazılma anı bilinmiyorsa (<c>default</c>) sapma <b>iddia edilmez</b>.
    ///
    /// <para>Zaman koşulu olmadan karar verilemez ve <c>DateTime.MinValue</c> her token'ı
    /// "kayıttan sonra" gösterir — yani sessizce <b>tüm</b> kullanıcılarda alarm çalardı.
    /// "Bilmiyorum" ile "bozuk" ayrı şeylerdir.</para>
    /// </summary>
    [Fact]
    public void Kayit_zamani_bilinmiyorsa_sapma_iddia_edilmez()
    {
        UserAccountDriftPolicy.Detect(
            recordRoles: [MesnetRoles.InstitutionManager],
            recordWrittenAt: default,
            tokenRoles: [MesnetRoles.InstitutionManager, MesnetRoles.SystemAdmin],
            tokenIssuedAt: SonrakiToken).ShouldBeEmpty();
    }

    /// <summary>
    /// Token rolü hiç okunamadıysa (claim yok, ayrıştırılamadı) sapma iddia edilmez —
    /// "bilmiyorum" ile "bozuk" ayrı şeylerdir.
    /// </summary>
    [Fact]
    public void Token_rolu_yoksa_sapma_iddia_edilmez()
    {
        UserAccountDriftPolicy.Detect(
            recordRoles: [MesnetRoles.Teacher],
            recordWrittenAt: KayitZamani,
            tokenRoles: [],
            tokenIssuedAt: SonrakiToken).ShouldBeEmpty();
    }

    /// <summary>
    /// Keycloak'ın kendi teknik rolleri (<c>offline_access</c>, <c>uma_authorization</c>,
    /// <c>default-roles-*</c>) her token'da bulunur ve <c>UserAccount</c>'a hiç yazılmaz.
    /// Süzülmeselerdi kontrol <b>her kullanıcıda</b> sürekli alarm çalardı — yani hiç
    /// çalışmazdı.
    /// </summary>
    [Theory]
    [InlineData("offline_access")]
    [InlineData("uma_authorization")]
    [InlineData("default-roles-mesnet")]
    [InlineData("manage-account")]
    public void Keycloak_teknik_rolleri_sapma_sayilmaz(string teknikRol)
    {
        UserAccountDriftPolicy.Detect(
            recordRoles: [MesnetRoles.Teacher],
            recordWrittenAt: KayitZamani,
            tokenRoles: [MesnetRoles.Teacher, teknikRol],
            tokenIssuedAt: SonrakiToken).ShouldBeEmpty();
    }

    /// <summary>
    /// MESNET rolü olmayan ama teknik listede de bulunmayan bir ad — ör. realm'e elle eklenmiş
    /// bir rol — sapma sayılmaz. Kontrol yalnız <b>projenin tanıdığı</b> roller hakkında
    /// konuşur; bilmediği bir ad için yönetici uyarmak yanlış alarmdır.
    /// </summary>
    [Fact]
    public void Tanimsiz_rol_adi_sapma_sayilmaz()
    {
        UserAccountDriftPolicy.Detect(
            recordRoles: [MesnetRoles.Teacher],
            recordWrittenAt: KayitZamani,
            tokenRoles: [MesnetRoles.Teacher, "elle-eklenmis-rol"],
            tokenIssuedAt: SonrakiToken).ShouldBeEmpty();
    }

    // ── Zaman damgası normalleştirme ──────────────────────────────────────────────────

    /// <summary>
    /// <b>Canlıda kontrolü tümden susturan hata.</b> Kayıt zamanı JSON'dan ofsetli gelirse
    /// (<c>...+00:00</c>) System.Text.Json onu <c>DateTimeKind.Local</c>'e çevirir; token zamanı
    /// <c>iat</c>'ten her zaman UTC üretilir. Ham karşılaştırmada pozitif ofsetli bölgelerde
    /// kayıt sürekli "daha yeni" görünür ve sapma <b>hiç bildirilmez</b>.
    ///
    /// <para>Gerçek ölçüm: kayıt 14:51Z verisi 17:51 yerel okundu, 15:57Z token'ı ondan eski
    /// sayıldı ve #205'in birebir tekrarı sessiz geçti.</para>
    /// </summary>
    [Fact]
    public void Yerel_kindli_kayit_zamani_UTC_tokenla_dogru_karsilastirilir()
    {
        var kayitUtc = new DateTime(2026, 8, 6, 14, 51, 0, DateTimeKind.Utc);
        var tokenUtc = new DateTime(2026, 8, 6, 15, 57, 0, DateTimeKind.Utc);

        var eksik = UserAccountDriftPolicy.Detect(
            recordRoles: [MesnetRoles.InstitutionManager],
            recordWrittenAt: kayitUtc.ToLocalTime(),   // ← aynı an, Local kind
            tokenRoles: [MesnetRoles.InstitutionManager, MesnetRoles.SystemAdmin],
            tokenIssuedAt: tokenUtc);

        eksik.ShouldBe([MesnetRoles.SystemAdmin],
            "Aynı an farklı Kind ile yazıldığında karar değişmemeli.");
    }

    /// <summary>Ters yön de korunmalı — Local kayıt gerçekten yeniyse sapma bildirilmemeli.</summary>
    [Fact]
    public void Yerel_kindli_yeni_kayit_yanlis_alarm_uretmez()
    {
        var tokenUtc = new DateTime(2026, 8, 6, 14, 0, 0, DateTimeKind.Utc);

        UserAccountDriftPolicy.Detect(
            recordRoles: [MesnetRoles.InstitutionManager],
            recordWrittenAt: tokenUtc.AddMinutes(30).ToLocalTime(),
            tokenRoles: [MesnetRoles.InstitutionManager, MesnetRoles.SystemAdmin],
            tokenIssuedAt: tokenUtc).ShouldBeEmpty();
    }

    /// <summary>Mesaj eyleme dönüşebilmeli: hangi kullanıcı, hangi rol, ne yapılmalı.</summary>
    [Fact]
    public void Aciklama_kullanici_rol_ve_duzeltme_yolunu_tasir()
    {
        var mesaj = UserAccountDriftPolicy.Describe("admin", [MesnetRoles.SystemAdmin]);

        mesaj.ShouldContain("admin");
        mesaj.ShouldContain(MesnetRoles.SystemAdmin);
        mesaj.ShouldContain("/api/security/users/sync");
    }
}
