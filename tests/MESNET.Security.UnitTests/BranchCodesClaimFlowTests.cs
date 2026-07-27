using System.Security.Claims;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Kayıtta girilen alanın token claim'ine akışı ve kapsam kararında kullanılması (#126).
///
/// <para>Zincir: kayıt (<c>CreateUser.BranchCodes</c>) → Keycloak <c>branch_codes</c>
/// özniteliği → token claim'i → <see cref="BranchCodeClaims.Read"/> →
/// <see cref="BranchScopePolicy.CanWrite"/>. Bu testler zincirin claim'den sonraki
/// kısmını uçtan uca doğrular.</para>
/// </summary>
public sealed class BranchCodesClaimFlowTests
{
    private static ClaimsPrincipal PrincipalWith(params string[] claimValues)
    {
        var identity = new ClaimsIdentity(
            claimValues.Select(v => new Claim(BranchCodeClaims.ClaimType, v)),
            authenticationType: "Test");

        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public void Kayitta_girilen_alan_claimden_okunur_ve_kapsam_kontrolunde_kullanilir()
    {
        // Keycloak multivalued mapper → aynı tipte birden çok claim
        var principal = PrincipalWith("EET");

        var codes = BranchCodeClaims.Read(principal);
        codes.ShouldBe(["EET"]);

        BranchScopePolicy.CanWrite("EET", codes, hasAllBranchesPermission: false).ShouldBeTrue();
        BranchScopePolicy.CanWrite("MTT", codes, hasAllBranchesPermission: false).ShouldBeFalse();
    }

    [Fact]
    public void Birden_cok_alan_claimi_listeye_donusur()
    {
        var codes = BranchCodeClaims.Read(PrincipalWith("EET", "MTT"));

        codes.ShouldBe(["EET", "MTT"]);
        BranchScopePolicy.CanWrite("MTT", codes, hasAllBranchesPermission: false).ShouldBeTrue();
    }

    /// <summary>Bazı mapper yapılandırmaları tek claim'de JSON dizi ya da virgüllü metin üretir.</summary>
    [Theory]
    [InlineData("[\"EET\",\"MTT\"]")]
    [InlineData("EET,MTT")]
    [InlineData("EET, MTT")]
    public void Dizi_ve_virgullu_bicimler_de_ayristirilir(string rawClaimValue)
    {
        var codes = BranchCodeClaims.Read(PrincipalWith(rawClaimValue));

        codes.ShouldBe(["EET", "MTT"]);
    }

    /// <summary>
    /// Alan güncellendiğinde yeni alana yazılabilir, eskisine yazılamaz — kapsam
    /// değişiminin gerçekten etki ettiğini doğrular.
    /// </summary>
    [Fact]
    public void Alan_guncellendiginde_yeni_alana_yazilir_eskisine_yazilamaz()
    {
        var before = BranchCodeClaims.Read(PrincipalWith("EET"));
        BranchScopePolicy.CanWrite("EET", before, hasAllBranchesPermission: false).ShouldBeTrue();
        BranchScopePolicy.CanWrite("MTT", before, hasAllBranchesPermission: false).ShouldBeFalse();

        // İdare kullanıcıyı MTT alanına taşıdı → yeni token/claim
        var after = BranchCodeClaims.Read(PrincipalWith("MTT"));
        BranchScopePolicy.CanWrite("MTT", after, hasAllBranchesPermission: false).ShouldBeTrue();
        BranchScopePolicy.CanWrite("EET", after, hasAllBranchesPermission: false).ShouldBeFalse();
    }

    [Fact]
    public void Claim_hic_yoksa_bos_liste_doner_ve_bu_hata_degildir()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "Test"));

        var codes = BranchCodeClaims.Read(principal);
        codes.ShouldBeEmpty();

        // Muafiyeti olan yönetici bu durumda da her alana yazabilir
        BranchScopePolicy.CanWrite("EET", codes, hasAllBranchesPermission: true).ShouldBeTrue();
        // Muafiyeti olmayan kullanıcı yazamaz
        BranchScopePolicy.CanWrite("EET", codes, hasAllBranchesPermission: false).ShouldBeFalse();
    }
}
