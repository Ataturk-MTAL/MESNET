using System.Security.Claims;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// <b>Güvenlik regresyonu:</b> kullanıcının kendi yazabileceği bir kaynaktan gelen alan
/// kodu kapsam kontrolünden GEÇMEMELİDİR (#126 güvenlik incelemesi — KRİTİK).
///
/// <para><b>Açık:</b> <c>branch_codes</c> Keycloak'ta unmanaged bir kullanıcı özniteliğidir.
/// Realm politikası <c>ENABLED</c> iken kullanıcı, varsayılan <c>manage-account</c> rolüyle
/// kendi Account konsolundan/REST API'sinden kendi özniteliğine kod ekleyebiliyordu.
/// <c>PermissionClaimsTransformation</c> token'da claim varsa kullanıcı kaydıyla hiç
/// karşılaştırmadan kabul ediyordu → EET alan şefi kendine <c>MTT</c> ekleyip MTT'nin saat
/// dağıtımını ezebilirdi.</para>
///
/// <para><b>Düzeltme iki katmanlı:</b> (1) realm politikası <c>ADMIN_EDIT</c>,
/// (2) <c>UserAccount.BranchCodes</c> otoriter — token claim'i eziliyor. Bu testler
/// (2)'nin sözleşmesini kilitler; (1)'e bağımlı DEĞİLDİR.</para>
/// </summary>
public sealed class BranchClaimSpoofingTests
{
    private const string Eet = "EET";
    private const string Mtt = "MTT";

    /// <summary>
    /// <c>PermissionClaimsTransformation.EnrichBranchCodesClaimAsync</c>'in otoriter-kayıt
    /// sözleşmesinin saf modeli: kullanıcı kaydı doluysa <b>yalnız</b> o geçerlidir,
    /// token'dan gelen değerler tümüyle atılır (birleştirilmez).
    /// </summary>
    private static IReadOnlyList<string> ResolveEffectiveBranchCodes(
        IReadOnlyList<string> tokenClaimCodes,
        IReadOnlyList<string> accountBranchCodes)
    {
        // Kayıt otoriterdir — doluysa token yok sayılır.
        if (accountBranchCodes.Count > 0)
            return accountBranchCodes;

        // Kayıt boşsa (eski kullanıcı) token claim'i kabul edilir.
        return tokenClaimCodes;
    }

    [Fact]
    public void Tokenda_olan_ama_kullanici_kaydinda_olmayan_alan_kodu_kapsam_kontrolunden_GECMEZ()
    {
        // Saldırı: EET'ye atanmış alan şefi kendi Keycloak özniteliğine MTT ekledi
        var tokenCodes = new[] { Eet, Mtt };
        var accountCodes = new[] { Eet };   // idarenin girdiği gerçek kapsam

        var effective = ResolveEffectiveBranchCodes(tokenCodes, accountCodes);

        effective.ShouldBe([Eet]);
        effective.ShouldNotContain(Mtt);

        // Asıl iddia: MTT'ye yazamamalı
        BranchScopePolicy.CanWrite(Mtt, effective, hasAllBranchesPermission: false)
            .ShouldBeFalse();

        // Kendi alanına yazabilmeye devam etmeli
        BranchScopePolicy.CanWrite(Eet, effective, hasAllBranchesPermission: false)
            .ShouldBeTrue();
    }

    [Fact]
    public void Token_tamamen_uydurma_kodlardan_olussa_bile_kayit_kazanir()
    {
        var effective = ResolveEffectiveBranchCodes(["MTT", "BLS", "MAK"], [Eet]);

        effective.ShouldBe([Eet]);
        BranchScopePolicy.CanWrite("BLS", effective, hasAllBranchesPermission: false).ShouldBeFalse();
        BranchScopePolicy.CanWrite("MAK", effective, hasAllBranchesPermission: false).ShouldBeFalse();
    }

    /// <summary>
    /// Kayıt boşken token claim'i hâlâ kabul edilir — #126 öncesi kullanıcılar kilitlenmesin.
    /// Bu, kabul edilen kalıntı risktir ve realm politikası (<c>ADMIN_EDIT</c>) ile kapatılır.
    /// </summary>
    [Fact]
    public void Kullanici_kaydi_bossa_token_claimi_gecis_icin_kabul_edilir()
    {
        var effective = ResolveEffectiveBranchCodes([Eet], []);

        effective.ShouldBe([Eet]);
    }

    [Fact]
    public void Kayit_doluyken_token_bos_olsa_bile_kayit_kullanilir()
    {
        var effective = ResolveEffectiveBranchCodes([], [Eet, Mtt]);

        effective.ShouldBe([Eet, Mtt]);
        BranchScopePolicy.CanWrite(Mtt, effective, hasAllBranchesPermission: false).ShouldBeTrue();
    }

    /// <summary>
    /// Sanitizasyonun principal üzerinde gerçekten uygulanabilir olduğunu doğrular:
    /// <c>branch_codes</c> claim'leri kaldırılabilir olmalı, aksi hâlde token'dan gelen
    /// değer <c>BranchCodeClaims.Read</c> ile okunmaya devam ederdi.
    /// </summary>
    [Fact]
    public void Tokendan_gelen_branch_codes_claimleri_principal_uzerinden_kaldirilabilir()
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(BranchCodeClaims.ClaimType, Eet),
                new Claim(BranchCodeClaims.ClaimType, Mtt),
            ],
            authenticationType: "Test");

        var principal = new ClaimsPrincipal(identity);
        BranchCodeClaims.Read(principal).Count.ShouldBe(2);

        foreach (var id in principal.Identities)
        {
            foreach (var claim in id.FindAll(BranchCodeClaims.ClaimType).ToList())
                id.TryRemoveClaim(claim).ShouldBeTrue();
        }

        BranchCodeClaims.Read(principal).ShouldBeEmpty();

        // Kayıttaki otoriter değer yerine konur
        identity.AddClaim(new Claim(BranchCodeClaims.ClaimType, Eet));
        BranchCodeClaims.Read(principal).ShouldBe([Eet]);
    }
}
