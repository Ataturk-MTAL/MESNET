using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Okula ilk yöneticiyi bağlama koşulu (B parçası).
///
/// <para><b>Neden koşullu:</b> "müdahale" tıkanıklığı fiilen varken açmaktır. Okulun
/// yöneticisi olduğu anda kapı kapanır ve yetki okula döner. Koşulsuz bir izin, il
/// yetkilisine alt ağaçtaki her okulun kullanıcı bağlarını süresiz değiştirme yetkisi
/// verirdi — istenen şeyden kat kat geniş.</para>
///
/// <para><b>Üç koşul da BİRLİKTE aranır.</b> Herhangi biri düşerse bootstrap yolu kapalıdır;
/// çağıran normal kapsam kuralına (<c>UserInstitutionScopePolicy.CanAssign</c>) düşer.</para>
/// </summary>
public sealed class InstitutionBootstrapPolicyTests
{
    [Fact]
    public void Uc_kosul_saglaninca_baglanabilir()
    {
        InstitutionBootstrapPolicy.CanBootstrap(
            hasBootstrapPermission: true, targetInActorSubtree: true, targetHasManager: false)
            .ShouldBeTrue();
    }

    [Fact]
    public void Izin_yoksa_baglanamaz()
    {
        InstitutionBootstrapPolicy.CanBootstrap(false, true, false).ShouldBeFalse();
    }

    [Fact]
    public void Hedef_alt_agac_disindaysa_baglanamaz()
    {
        InstitutionBootstrapPolicy.CanBootstrap(true, false, false).ShouldBeFalse();
    }

    [Fact]
    public void Okulun_yoneticisi_VARSA_baglanamaz()
    {
        // Kapının kapandığı an. Tıkanıklık yoksa müdahale de yoktur.
        InstitutionBootstrapPolicy.CanBootstrap(true, true, targetHasManager: true)
            .ShouldBeFalse();
    }
}
