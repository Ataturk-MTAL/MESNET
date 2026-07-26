using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Coordination.UnitTests;

/// <summary>
/// Alan (branş) kapsamı kararının saf mantığı (#126).
///
/// <para>Kusur: permission erişimi açıyordu ama "hangi alanın verisi" sorusunu kimse
/// sormuyordu. EET alan şefi isteğe <c>branchCode=MTT</c> yazıp Makine Teknolojisi
/// alanının saat dağıtımını tek atomik çağrıda ezebiliyordu.</para>
/// </summary>
public sealed class BranchScopePolicyTests
{
    private const string Eet = "EET";
    private const string Mtt = "MTT";

    [Fact]
    public void Alan_sefi_kendi_alanina_yazabilir()
    {
        BranchScopePolicy.CanWrite(Eet, [Eet], hasAllBranchesPermission: false)
            .ShouldBeTrue();
    }

    [Fact]
    public void Alan_sefi_baska_alana_yazamaz()
    {
        BranchScopePolicy.CanWrite(Mtt, [Eet], hasAllBranchesPermission: false)
            .ShouldBeFalse();
    }

    [Fact]
    public void Birden_cok_alandan_sorumlu_kullanici_her_ikisine_de_yazabilir()
    {
        BranchScopePolicy.CanWrite(Eet, [Eet, Mtt], hasAllBranchesPermission: false).ShouldBeTrue();
        BranchScopePolicy.CanWrite(Mtt, [Eet, Mtt], hasAllBranchesPermission: false).ShouldBeTrue();
    }

    /// <summary>
    /// Senaryonun kalbi: <b>yöneticinin branş kodu yoktur ve bu doğru durumdur</b> —
    /// veri eksikliği değildir. Muafiyet izni varken alan listesine HİÇ bakılmaz;
    /// listeyi önce kontrol eden bir sıralama yöneticiyi kilitlerdi.
    /// </summary>
    [Theory]
    [InlineData(Eet)]
    [InlineData(Mtt)]
    [InlineData("HERHANGI")]
    public void Muafiyet_izni_olan_kullanici_alan_listesi_TAMAMEN_BOSKEN_her_alana_yazabilir(string branchCode)
    {
        BranchScopePolicy.CanWrite(branchCode, [], hasAllBranchesPermission: true).ShouldBeTrue();
        BranchScopePolicy.CanWrite(branchCode, null, hasAllBranchesPermission: true).ShouldBeTrue();
    }

    [Fact]
    public void Muafiyet_izni_olan_kullanici_listesinde_olmayan_alana_da_yazabilir()
    {
        BranchScopePolicy.CanWrite(Mtt, [Eet], hasAllBranchesPermission: true).ShouldBeTrue();
        BranchScopePolicy.CanWrite(null, null, hasAllBranchesPermission: true).ShouldBeTrue();
    }

    /// <summary>
    /// Boş liste yalnız <b>muafiyeti olmayan</b> kullanıcı için kısıtlayıcıdır.
    /// Alan şefinin personel kaydında branş kodu yoksa yazmaya kilitlenir — bu,
    /// yöneticinin boş listesinden farklı bir durumdur ve dolgu yolunun neden
    /// çalışır gelmesi gerektiğinin sebebidir.
    /// </summary>
    [Fact]
    public void Muafiyeti_olmayan_kullanici_alan_listesi_bossa_hicbir_alana_yazamaz()
    {
        BranchScopePolicy.CanWrite(Eet, [], hasAllBranchesPermission: false).ShouldBeFalse();
        BranchScopePolicy.CanWrite(Eet, null, hasAllBranchesPermission: false).ShouldBeFalse();
    }

    /// <summary>
    /// Hedef alan boşsa kapsam bilinmiyordur. İzin verilseydi, isteğe <c>branchCode</c>
    /// hiç yazmayarak kontrol atlatılırdı.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Hedef_alan_bos_ise_muafiyet_olmadan_yazamaz(string? branchCode)
    {
        BranchScopePolicy.CanWrite(branchCode, [Eet], hasAllBranchesPermission: false)
            .ShouldBeFalse();
    }

    [Theory]
    [InlineData("eet")]
    [InlineData("Eet")]
    [InlineData(" EET ")]
    public void Alan_kodu_karsilastirmasi_buyuk_kucuk_harf_ve_bosluga_duyarsizdir(string requested)
    {
        BranchScopePolicy.CanWrite(requested, [Eet], hasAllBranchesPermission: false)
            .ShouldBeTrue();
    }
}
