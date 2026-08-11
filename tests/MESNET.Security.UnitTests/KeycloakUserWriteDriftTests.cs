using System.Text.RegularExpressions;
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Keycloak kullanıcı yazma yolları ikiye ayrılır ve ayrım korunmalıdır (ADR-0003 adım 3).
///
/// <para><b>Ölçülen kural (Keycloak 26.7.0):</b> PUT gövdesinde <c>attributes</c> varsa istek
/// <i>tam profil yazımı</i> sayılır — gövdede geçmeyen <c>firstName</c>/<c>email</c> <b>silinir</b>
/// ve öznitelik haritası tümüyle gönderilenle değişir. Yoksa istek kısmi kalır ve diğer alanlara
/// dokunulmaz. Aynı kullanıcıda arka arkaya ölçüldü:</para>
/// <code>
/// PUT {"enabled":false}                       → firstName=Deneme  email=a@…  attrs=2 alan  (204)
/// PUT {"attributes":{"branch_codes":["EET"]}} → firstName=NULL    email=NULL attrs=1 alan  (204)
/// </code>
///
/// <para><b>Neden kilit gerekiyor:</b> iki istek de <b>204</b> döner. Kayıp ne çağıranda ne
/// logda görünür; ancak haftalar sonra "personel listesinde ad sütunu boş" diye ortaya çıkar
/// (#190). Derleyici de göremez: <c>attributes</c> anahtarını kısmi bir sözlüğe eklemek geçerli
/// C#'tır.</para>
///
/// <para><b>Doğru ayrım:</b> profil alanları <c>PatchUserFieldsAsync</c> ile (kısmi, hızlı,
/// eşzamanlı güncellemeleri ezmez), öznitelikler <c>MergeUserAttributesAsync</c> ile (gövde taze
/// bir GET'ten kurulur, silinecek alan kalmaz).</para>
/// </summary>
public sealed class KeycloakUserWriteDriftTests
{
    private const string ServicePath =
        "Modules/Security/MESNET.Security.Application/Services/KeycloakAdminService.cs";

    /// <summary>
    /// <c>/users/{id}</c> hedefli PUT çağrıları. Kullanıcı temsilini yazan tek yer burasıdır;
    /// yeni bir yazma yolu eklenirse bu test onu görür.
    /// </summary>
    private static readonly Regex UserPut = new(
        @"SendAdminAsync\(\s*HttpMethod\.Put\s*,\s*\$""/users/", RegexOptions.Compiled);

    /// <summary>
    /// PUT'a izin verilen iki yol. Sayı sabittir: üçüncü bir yol eklemek, hangi semantiğin
    /// geçerli olduğuna dair <b>bilinçli</b> bir karar gerektirir.
    /// </summary>
    private const int AllowedUserPutSites = 2;

    [Fact]
    public void Kullanici_PUT_yolu_ikiden_fazla_degil()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot(), "src", ServicePath));

        UserPut.Matches(source).Count.ShouldBe(AllowedUserPutSites,
            "Keycloak kullanıcı temsiline yazan yeni bir PUT eklenmiş. İki semantik var ve "
            + "seçim sessizce yanlış yapılabilir: gövdede 'attributes' varsa Keycloak gövdede "
            + "geçmeyen firstName/email alanlarını 204 dönerek SİLER. Profil alanı için "
            + "PatchUserFieldsAsync, öznitelik için MergeUserAttributesAsync kullanın.");
    }

    [Fact]
    public void Attributes_iceren_govde_kismi_sayilmaz()
    {
        KeycloakUserWritePolicy.IsSafePartialBody(["enabled", "attributes"]).ShouldBeFalse();

        Should.Throw<ArgumentException>(
            () => KeycloakUserWritePolicy.EnsureSafePartialBody(["enabled", "attributes"]))
            .Message.ShouldContain("204");
    }

    [Fact]
    public void Profil_alanlari_kismi_govdeyle_gonderilebilir()
    {
        KeycloakUserWritePolicy.IsSafePartialBody(["email", "firstName", "lastName"]).ShouldBeTrue();
        Should.NotThrow(() => KeycloakUserWritePolicy.EnsureSafePartialBody(["enabled"]));
    }

    /// <summary>
    /// Kural <b>fırlatmalı</b>, süzmemeli: sessizce atılan bir öznitelik yazımı, hiç yazılmadan
    /// başarılı görünürdü — düzeltmeye çalıştığımız sessizliğin aynısı.
    /// </summary>
    [Fact]
    public void Ihlal_sessizce_suzulmez_firlatir()
    {
        var ex = Should.Throw<ArgumentException>(
            () => KeycloakUserWritePolicy.EnsureSafePartialBody([KeycloakUserWritePolicy.AttributesKey]));

        ex.Message.ShouldContain("#190");
    }

    /// <summary>
    /// Kısmi yazma yolu kuralı gerçekten <b>çağırmalı</b>. Bu bir kaynak taramasıdır ve
    /// bilerek dardır: kuralın kendisi yukarıdaki testlerle davranışsal olarak ölçülüyor,
    /// burada yalnız "çağrı yerinde duruyor mu" sorusu var.
    /// </summary>
    [Fact]
    public void Kismi_yazma_yolu_kurali_cagirir()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot(), "src", ServicePath));
        var patch = Between(source,
            "private async Task<Result> PatchUserFieldsAsync", "public async Task<Result> UpdateUserAsync");

        patch.Contains("KeycloakUserWritePolicy.EnsureSafePartialBody", StringComparison.Ordinal)
            .ShouldBeTrue("Kısmi yazma yolu artık kuralı çağırmıyor — koruma devre dışı kalmış olur.");
    }

    /// <summary>
    /// Öznitelik yazan yol gövdeyi <b>taze bir GET'ten</b> kurmalı. Merge kaldırılırsa öznitelik
    /// haritası tümüyle ezilir: bir kullanıcının <c>branch_codes</c>'u yazılırken
    /// <c>business_id</c>'si düşer — kapsam kararlarını sessizce değiştiren bir kayıp.
    /// </summary>
    [Fact]
    public void Oznitelik_yolu_once_GET_yapar()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot(), "src", ServicePath));
        var merge = Between(source, "private async Task<Result> MergeUserAttributesAsync", "// ── Listeleme");

        merge.Contains("HttpMethod.Get", StringComparison.Ordinal).ShouldBeTrue(
            "Öznitelik yazma yolu artık GET yapmıyor; gövde taze temsilden kurulmuyor.");
        merge.Contains("foreach (var prop in existing.EnumerateObject())", StringComparison.Ordinal).ShouldBeTrue(
            "GET'ten dönen temsil gövdeye kopyalanmıyor — yalnız 'attributes' göndermek "
            + "firstName/email alanlarını siler.");
    }

    private static string Between(string source, string start, string end)
    {
        var from = source.IndexOf(start, StringComparison.Ordinal);
        from.ShouldBeGreaterThanOrEqualTo(0, $"Kaynakta bulunamadı: {start}");

        var to = source.IndexOf(end, from, StringComparison.Ordinal);
        to.ShouldBeGreaterThanOrEqualTo(0, $"Kaynakta bulunamadı: {end}");

        return source[from..to];
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MESNET.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Depo kökü bulunamadı (MESNET.slnx aranıyordu): {AppContext.BaseDirectory}");
    }
}
