using System.Text.Json;
using MESNET.Seeder.Seeders;
using Shouldly;
using Xunit;

namespace MESNET.Seeder.UnitTests;

/// <summary>
/// Seeder okul verisini yalnız seed ettiği okulun kiracısında yazar (#309).
///
/// <para><b>Yaşanan hata:</b> seeder <c>admin</c> ile koşuyor. Dev veritabanında admin'in ev
/// kurumu il MEM'e bağlanmıştı; seeder bunu fark etmeden 6 öğretmeni il MEM kiracısına yazdı,
/// üstelik Atatürk MTAL etiketiyle. Mükerrerlik kontrolü de aktörün kiracısıyla süzülen listeye
/// baktığı için hiçbir şey görmedi — her koşu yeni kopya üretti.</para>
/// </summary>
public sealed class SeedTenantPolicyTests
{
    private static readonly Guid Okul = Guid.Parse("efd57b88-2f47-471c-9f51-476f80fabfca");

    private static JsonElement Me(string json) => JsonDocument.Parse(json).RootElement;

    [Fact]
    public void Kiraci_seed_edilen_okulsa_devam_edilir()
    {
        SeedTenantPolicy.Mismatch(Me("""{"tenantId":"efd57b88-2f47-471c-9f51-476f80fabfca"}"""), Okul)
            .ShouldBeNull();
    }

    /// <summary><b>Asıl regresyon:</b> başka kurumun kiracısı → dur, sebebini söyle.</summary>
    [Fact]
    public void Kiraci_baska_kurumsa_sebep_doner()
    {
        var reason = SeedTenantPolicy.Mismatch(
            Me("""{"tenantId":"22df21ed-dd96-4026-bcdd-c351199e3692"}"""), Okul);

        reason.ShouldNotBeNull();
        reason.ShouldContain("22df21ed-dd96-4026-bcdd-c351199e3692");
    }

    [Theory]
    [InlineData("""{"tenantId":null}""")]
    [InlineData("""{"tenantId":"platform"}""")]
    [InlineData("""{}""")]
    public void Kiraci_yoksa_ya_da_okul_degilse_sebep_doner(string json)
    {
        SeedTenantPolicy.Mismatch(Me(json), Okul).ShouldNotBeNull();
    }

    [Fact]
    public void Kimlik_bicimi_buyuk_harfe_duyarsizdir()
    {
        SeedTenantPolicy.Mismatch(Me("""{"tenantId":"EFD57B88-2F47-471C-9F51-476F80FABFCA"}"""), Okul)
            .ShouldBeNull();
    }
}
