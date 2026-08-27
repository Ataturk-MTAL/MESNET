using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Materyalize yolun BİÇİMİ bir güvenlik kararıdır, süs değil.
///
/// <para>Sondaki ayraç olmadan <c>/33/1</c> öneki <c>/33/10...</c> yolunu da yakalar ve bir
/// ilçe yetkilisi KARDEŞ ilçenin okullarını görür. Kimlikler Guid olduğu için bugün segment
/// uzunlukları sabittir ve bu çakışma pratikte oluşamaz — ama biçim garantisi kimlik tipine
/// bağlı bırakılmaz: yarın kısa bir kod segmenti eklenirse kural sessizce çöker.</para>
/// </summary>
public sealed class InstitutionPathTests
{
    private static readonly Guid Il = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Ilce = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Okul = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void Kok_yolu_iki_ayrac_arasinda_kimlik_tasir()
    {
        InstitutionPath.Root(Il).ShouldBe($"/{Il:D}/");
    }

    [Fact]
    public void Cocuk_yolu_ust_yolun_uzerine_eklenir()
    {
        var ilce = InstitutionPath.Child(InstitutionPath.Root(Il), Ilce);
        var okul = InstitutionPath.Child(ilce, Okul);

        okul.ShouldBe($"/{Il:D}/{Ilce:D}/{Okul:D}/");
    }

    [Fact]
    public void Ust_yolu_bos_olan_cocuk_yaratilamaz()
    {
        Should.Throw<ArgumentException>(() => InstitutionPath.Child("", Okul));
        Should.Throw<ArgumentException>(() => InstitutionPath.Child("   ", Okul));
    }

    [Theory]
    [InlineData("a", "/a/")]
    [InlineData("/a", "/a/")]
    [InlineData("a/", "/a/")]
    [InlineData("  /a/  ", "/a/")]
    public void Normalize_bastaki_ve_sondaki_ayraci_garanti_eder(string girdi, string beklenen)
    {
        InstitutionPath.Normalize(girdi).ShouldBe(beklenen);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_bos_degeri_null_dondurur(string? girdi)
    {
        InstitutionPath.Normalize(girdi).ShouldBeNull();
    }

    [Fact]
    public void Dugum_kendi_alt_agacindadir()
    {
        InstitutionPath.Contains("/a/b/", "/a/b/").ShouldBeTrue();
    }

    [Fact]
    public void Alt_ve_torun_dugum_kapsamdadir()
    {
        InstitutionPath.Contains("/a/", "/a/b/").ShouldBeTrue();
        InstitutionPath.Contains("/a/", "/a/b/c/").ShouldBeTrue();
    }

    [Fact]
    public void Ust_dugum_kapsam_DISIDIR()
    {
        InstitutionPath.Contains("/a/b/", "/a/").ShouldBeFalse(
            "Okul müdürü ilçe müdürlüğünün kaydını görmemeli — kapsam aşağı doğrudur.");
    }

    [Fact]
    public void Kardes_dugum_kapsam_disidir()
    {
        InstitutionPath.Contains("/a/b/", "/a/c/").ShouldBeFalse();
    }

    /// <summary>
    /// Biçimin var oluş nedeni. Ayraçla bitmeyen bir önek karşılaştırması burada
    /// <c>true</c> döner ve kardeş ilçe sızar.
    /// </summary>
    [Fact]
    public void Onek_benzerligi_kardes_dugumu_sizdirmaz()
    {
        InstitutionPath.Contains("/33/1/", "/33/10/").ShouldBeFalse();
    }

    [Theory]
    [InlineData(null, "/a/")]
    [InlineData("/a/", null)]
    [InlineData("", "/a/")]
    [InlineData("/a/", "")]
    public void Bos_yol_hicbir_seyi_kapsamaz(string? ata, string? torun)
    {
        InstitutionPath.Contains(ata, torun).ShouldBeFalse();
    }
}
