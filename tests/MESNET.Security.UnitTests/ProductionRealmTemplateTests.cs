using Xunit;
using System.Text.Json;
using MESNET.Common.Shared.Security;
using Shouldly;

namespace MESNET.Security.UnitTests;

/// <summary>
/// <c>deploy/keycloak/mesnet-realm.production.json</c> üretim kurulumunun realm kaynağıdır ve
/// koddaki değişmezlerle BİREBİR aynı kalmak zorundadır.
///
/// <para><b>Neden ayrı bir kilit gerekiyor:</b> geliştirme realm'i
/// (<c>src/MESNET.AppHost/keycloak/mesnet-realm.json</c>) zaten kilitli. Ama üretim şablonu ondan
/// AYRI bir dosyadır; iki dosya ayrı yaşadığı sürece sapma kaçınılmazdır — yeni bir rol dev
/// realm'ine eklenir, üretimde unutulur ve eksiklik yalnız gerçek kurulumda ortaya çıkar. Orada da
/// belirti hata değildir: <c>RealmVerificationHostedService</c> üretimde <b>fırlatmaz</b>, yalnız
/// <c>LogCritical</c> yazar (Development'ta durur). Yani eksik rol, kimse logu okumazsa
/// dağıtımdan aylar sonra "o kullanıcı neden yetkisiz" sorusu olarak döner.</para>
///
/// <para>Bu dosya ayrıca <b>sırsız</b> olmalıdır: depo PUBLIC'tir ve şablona düşen bir parola ya
/// da client secret geri alınamaz.</para>
/// </summary>
public class ProductionRealmTemplateTests
{
    private const string TemplatePath = "mesnet-realm.production.json";

    /// <summary>Alan adı, kurulum betiği tarafından yazılan yer tutucu.</summary>
    private const string DomainPlaceholder = "__APP_DOMAIN__";

    private static JsonElement Realm() =>
        JsonDocument.Parse(File.ReadAllText(TemplatePath)).RootElement;

    private static IReadOnlyList<string> RealmRoleNames() =>
        [.. Realm().GetProperty("roles").GetProperty("realm")
            .EnumerateArray()
            .Select(r => r.GetProperty("name").GetString()!)];

    private static JsonElement Client(string clientId) =>
        Realm().GetProperty("clients").EnumerateArray()
            .Single(c => c.GetProperty("clientId").GetString() == clientId);

    [Fact]
    public void Sablon_MesnetRoles_ile_birebir_ayni_rolleri_tanimlar()
    {
        var sablondaki = RealmRoleNames();

        // İki yönlü: eksik rol yetkiyi hiç doğurmaz, fazla rol atanabilir ama izin haritasında
        // karşılığı yoktur — ikisi de sessizdir.
        sablondaki.ShouldBe(MesnetRoles.All, ignoreOrder: true);
    }

    [Fact]
    public void Sablon_realm_adi_ve_web_client_kimligi_kodla_ayni()
    {
        Realm().GetProperty("realm").GetString().ShouldBe("mesnet");
        Client(RealmInvariants.WebClientId).GetProperty("clientId").GetString()
            .ShouldBe(RealmInvariants.WebClientId);
    }

    [Fact]
    public void Web_client_public_olmalidir_PKCE_akisi_secret_tasiyamaz()
    {
        Client(RealmInvariants.WebClientId).GetProperty("publicClient").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public void Unmanaged_oznitelik_politikasi_ADMIN_EDIT_olmalidir()
    {
        // ENABLED olsaydı kullanıcı `manage-account` ile kendi Account konsolundan kendine
        // branch_codes ekleyip kapsamını aşabilirdi (#126). Politika şablondan gelmezse
        // doğrulayıcı da sessiz kalır: alan boşsa sapma ÜRETİLMEZ.
        File.ReadAllText(TemplatePath)
            .ShouldContain(RealmInvariants.ExpectedUnmanagedAttributePolicy);
    }

    [Fact]
    public void Sablonda_HICBIR_kimlik_bilgisi_bulunmaz()
    {
        var realm = Realm();

        foreach (var client in realm.GetProperty("clients").EnumerateArray())
        {
            client.TryGetProperty("secret", out _).ShouldBeFalse(
                $"client '{client.GetProperty("clientId").GetString()}' şablonda secret taşıyor; " +
                "üretim secret'ını Keycloak üretir ve kurulum betiği okur.");
        }

        if (realm.TryGetProperty("users", out var users))
        {
            foreach (var user in users.EnumerateArray())
            {
                user.TryGetProperty("credentials", out _).ShouldBeFalse(
                    $"kullanıcı '{user.GetProperty("username").GetString()}' şablonda parola taşıyor.");
            }
        }
    }

    [Fact]
    public void Sablonda_gelistirme_tohum_kullanicisi_bulunmaz()
    {
        var realm = Realm();
        if (!realm.TryGetProperty("users", out var users))
        {
            return;
        }

        // Kalması gereken tek kayıt servis hesabıdır: realm-management rolleri onun üzerinde
        // tanımlıdır ve düşerse Admin API yetkisi hiç doğmaz. İnsan hesapları üretimde
        // operatör tarafından açılır.
        foreach (var user in users.EnumerateArray())
        {
            user.TryGetProperty("serviceAccountClientId", out _).ShouldBeTrue(
                $"'{user.GetProperty("username").GetString()}' bir tohum kullanıcısı; " +
                "üretim şablonunda bulunmamalı.");
        }

        // RealmInvariants bu adları BULUNURSA rollerini denetler; üretimde hiçbiri olmamalıdır.
        var adlar = users.EnumerateArray()
            .Select(u => u.GetProperty("username").GetString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var tohum in RealmInvariants.ExpectedSeedUserRoles.Keys)
        {
            adlar.ShouldNotContain(tohum);
        }
    }

    [Fact]
    public void Uretimde_TLS_zorunlu_olmalidir()
    {
        // 'none' olsaydı Keycloak parolayı düz HTTP üzerinden de kabul ederdi.
        Realm().GetProperty("sslRequired").GetString().ShouldBe("external");
    }

    [Fact]
    public void Web_client_adresleri_alan_adi_yer_tutucusu_tasir()
    {
        var web = Client(RealmInvariants.WebClientId);

        foreach (var alan in (string[])["redirectUris", "webOrigins"])
        {
            var degerler = web.GetProperty(alan).EnumerateArray()
                .Select(v => v.GetString()!).ToList();

            degerler.ShouldNotBeEmpty($"'{alan}' boşsa üretimde giriş akışı tamamlanamaz.");
            degerler.ShouldAllBe(v => v.Contains(DomainPlaceholder, StringComparison.Ordinal),
                $"'{alan}' sabit bir adres taşıyor; kurulum betiği onu alan adıyla değiştiremez. " +
                "localhost kalırsa gerçek alan adından gelen giriş Keycloak tarafından reddedilir.");
        }
    }
}
