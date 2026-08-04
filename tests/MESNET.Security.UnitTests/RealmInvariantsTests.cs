using System.Text.Json;
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Realm sapma doğrulamasının karar mantığı (#195).
///
/// <para><b>Neden var:</b> Keycloak realm import <b>tek seferliktir</b> — yalnız boş veritabanında
/// çalışır. Depodaki <c>mesnet-realm.json</c>'a sonradan eklenen ayarlar mevcut bir kaba hiç
/// ulaşmaz. Canlı dev realm'inde <c>unmanagedAttributePolicy</c> <c>ENABLED</c> bulundu; depoda
/// <c>ADMIN_EDIT</c> yazıyordu ve #126'nın ikinci savunma katmanı o ortamda hiç aktif olmamıştı.</para>
///
/// <para>Sapmayı hiçbir birim testi göremez — testler depodaki dosyayı okur, çalışan realm'i
/// değil. Bu yüzden karar mantığı burada, okuma ise açılış kontrolünde durur. Bu sınıf kararı
/// kilitler.</para>
/// </summary>
public sealed class RealmInvariantsTests
{
    /// <summary>Depodaki tanımla uyumlu realm — hiçbir sapma üretmemeli.</summary>
    private static RealmSnapshot Saglikli() => new(
        UnmanagedAttributePolicy: RealmInvariants.ExpectedUnmanagedAttributePolicy,
        RealmRoles: [.. MesnetRoles.All],
        WebClientIsPublic: true);

    [Fact]
    public void Depodaki_tanimla_uyumlu_realm_sapma_uretmez()
    {
        RealmInvariants.Verify(Saglikli()).ShouldBeEmpty();
    }

    // ── Yakalanması gereken sapmalar ────────────────────────────────────────────────────

    /// <summary>Canlı ortamda gerçekten bulunan sapma.</summary>
    [Fact]
    public void Unmanaged_oznitelik_politikasi_ENABLED_ise_sapma_bildirilir()
    {
        var drifts = RealmInvariants.Verify(
            Saglikli() with { UnmanagedAttributePolicy = "ENABLED" });

        var drift = drifts.ShouldHaveSingleItem();
        drift.Key.ShouldBe("unmanagedAttributePolicy");
        drift.Expected.ShouldBe("ADMIN_EDIT");
        drift.Actual.ShouldBe("ENABLED");

        // Mesaj eyleme dönüşebilmeli: hem sonuç hem düzeltme yolu.
        drift.Impact.ShouldContain("branch_codes");
        drift.Impact.ShouldContain("users/profile");
    }

    [Fact]
    public void Realm_rolu_eksikse_sapma_bildirilir()
    {
        var eksikRoller = MesnetRoles.All.Where(r => r != MesnetRoles.Parent).ToList();

        var drift = RealmInvariants.Verify(Saglikli() with { RealmRoles = eksikRoller })
            .ShouldHaveSingleItem();

        drift.Key.ShouldBe("realm roles");
        drift.Actual.ShouldContain(MesnetRoles.Parent);
    }

    [Fact]
    public void Web_client_public_degilse_sapma_bildirilir()
    {
        var drift = RealmInvariants.Verify(Saglikli() with { WebClientIsPublic = false })
            .ShouldHaveSingleItem();

        drift.Key.ShouldContain(RealmInvariants.WebClientId);
        drift.Impact.ShouldContain("PKCE");
    }

    [Fact]
    public void Birden_cok_sapma_ayri_ayri_bildirilir()
    {
        var drifts = RealmInvariants.Verify(new RealmSnapshot(
            UnmanagedAttributePolicy: "ENABLED",
            RealmRoles: [],
            WebClientIsPublic: false));

        // Roller boş liste — "okunamadı" sayılır, sapma DEĞİL (aşağıdaki teste bakınız).
        drifts.Select(d => d.Key).ShouldBe(
            ["unmanagedAttributePolicy", $"client {RealmInvariants.WebClientId}.publicClient"],
            ignoreOrder: true);
    }

    // ── Yanlış alarm üretmemesi gereken durumlar ───────────────────────────────────────

    /// <summary>
    /// Okunamayan alan sapma DEĞİLDİR. Servis hesabının yetkisi eksikse ya da Keycloak sürümü
    /// alanı başka adla veriyorsa, "bilmiyorum" ile "bozuk" karıştırılmamalı — yoksa her açılışta
    /// yanlış alarm çalar ve kontrol güvenilirliğini kaybeder.
    /// </summary>
    [Fact]
    public void Okunamayan_alanlar_sapma_sayilmaz()
    {
        var bosGoruntu = new RealmSnapshot(UnreadableFields: ["users/profile (HTTP 403)"]);

        RealmInvariants.Verify(bosGoruntu).ShouldBeEmpty();
    }

    [Fact]
    public void Politika_buyuk_kucuk_harf_farkiyla_yazilmissa_sapma_sayilir()
    {
        // Keycloak değeri sabit biçimde döndürür; farklı yazım gerçekten farklı bir değerdir.
        RealmInvariants.Verify(Saglikli() with { UnmanagedAttributePolicy = "admin_edit" })
            .ShouldHaveSingleItem().Key.ShouldBe("unmanagedAttributePolicy");
    }

    [Fact]
    public void Rol_adi_buyuk_kucuk_harfe_duyarsiz_eslesir()
    {
        var farkliYazim = MesnetRoles.All.Select(r => r.ToUpperInvariant()).ToList();

        RealmInvariants.Verify(Saglikli() with { RealmRoles = farkliYazim }).ShouldBeEmpty();
    }

    /// <summary>Realm'de fazladan rol bulunması sapma değildir — beklenenler var, yeter.</summary>
    [Fact]
    public void Fazladan_realm_rolu_sapma_sayilmaz()
    {
        List<string> fazlali = [.. MesnetRoles.All, "offline_access", "uma_authorization"];

        RealmInvariants.Verify(Saglikli() with { RealmRoles = fazlali }).ShouldBeEmpty();
    }

    // ── Depodaki realm tanımıyla tutarlılık ────────────────────────────────────────────

    /// <summary>
    /// Beklenen değerler depodaki realm tanımıyla aynı olmalı. Biri değişip diğeri unutulursa
    /// doğrulama <b>kendi kaynağından</b> sapar: realm'i olması gerektiği gibi kurmuş bir ortam
    /// yanlışlıkla "sapmış" damgası yer.
    /// </summary>
    [Fact]
    public void Beklenen_politika_depodaki_realm_tanimiyla_ayni()
    {
        var realm = JsonDocument.Parse(File.ReadAllText("mesnet-realm.json")).RootElement;

        var profilJson = realm
            .GetProperty("components")
            .GetProperty("org.keycloak.userprofile.UserProfileProvider")[0]
            .GetProperty("config")
            .GetProperty("kc.user.profile.config")[0]
            .GetString();

        var politika = JsonDocument.Parse(profilJson!).RootElement
            .GetProperty("unmanagedAttributePolicy").GetString();

        politika.ShouldBe(
            RealmInvariants.ExpectedUnmanagedAttributePolicy,
            "mesnet-realm.json ile RealmInvariants aynı değeri söylemeli.");
    }

    [Fact]
    public void Web_client_depodaki_realm_tanimda_public()
    {
        var realm = JsonDocument.Parse(File.ReadAllText("mesnet-realm.json")).RootElement;

        var webClient = realm.GetProperty("clients").EnumerateArray()
            .Single(c => c.GetProperty("clientId").GetString() == RealmInvariants.WebClientId);

        webClient.GetProperty("publicClient").GetBoolean().ShouldBeTrue();
    }
}
