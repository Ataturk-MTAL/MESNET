using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Kullanıcı ve davet okumalarında kapsamın hangi dala düştüğü.
///
/// <para><b>Neden saf fonksiyon:</b> gerçek daraltmayı ölçen uçtan uca test bu depoda
/// yazılamıyor — <c>MESNET.Api.Tests</c> çalışan yığına karşı koşar ve realm'de ikinci kuruma
/// bağlı kullanıcı yoktur. Karar buraya çıkarıldığı için DB'siz ve Keycloak'sız ölçülebilir.</para>
///
/// <para><b>En olası sessiz hata platform muafiyetinin sırasını kaçırmaktır:</b> kendi kurumu
/// olmayan platform aktörü <c>Guid.Empty</c>'ye düşerse HER ZAMAN boş liste görür ve bu hata
/// vermez.</para>
/// </summary>
public sealed class UserScopePolicyTests
{
    private static readonly Guid OkulA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OkulB = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Platform_kapsami_suzgec_uygulatmaz()
    {
        var scope = new InstitutionVisibility(Unrestricted: true, PathPrefix: null, InstitutionId: null);

        UserScopePolicy.VisibleInstitutionIds(scope, []).ShouldBeNull();
    }

    /// <summary>
    /// Sıra kritik: platform aktörünün kurumu OLMAYABİLİR. Muafiyet en önde
    /// değerlendirilmezse kimlik dalına düşer ve her zaman boş liste görür.
    /// </summary>
    [Fact]
    public void Platform_kapsami_kurum_kimligi_dolu_olsa_bile_en_onde()
    {
        var scope = new InstitutionVisibility(Unrestricted: true, PathPrefix: "/il-35", InstitutionId: OkulA);

        UserScopePolicy.VisibleInstitutionIds(scope, [OkulB]).ShouldBeNull();
    }

    [Fact]
    public void Yol_oneki_olan_aktor_alt_agac_kimliklerini_gorur()
    {
        var scope = new InstitutionVisibility(Unrestricted: false, PathPrefix: "/il-35/ilce-konak", InstitutionId: null);

        UserScopePolicy.VisibleInstitutionIds(scope, [OkulA, OkulB]).ShouldBe([OkulA, OkulB]);
    }

    /// <summary>
    /// Alt ağaç boş dönerse kapsam BOŞ kümedir — "her şey" değil. Boş kümede yalnız kurum
    /// bağı olmayan kayıtlar görünür; bu Karar 3'ün gereğidir.
    /// </summary>
    [Fact]
    public void Yol_oneki_var_ama_alt_agac_bossa_bos_kume_doner()
    {
        var scope = new InstitutionVisibility(Unrestricted: false, PathPrefix: "/il-35/ilce-konak", InstitutionId: null);

        UserScopePolicy.VisibleInstitutionIds(scope, []).ShouldBeEmpty();
    }

    [Fact]
    public void Yolu_olmayan_okul_aktoru_yalniz_kendi_kurumunu_gorur()
    {
        var scope = new InstitutionVisibility(Unrestricted: false, PathPrefix: null, InstitutionId: OkulA);

        UserScopePolicy.VisibleInstitutionIds(scope, []).ShouldBe([OkulA]);
    }

    [Fact]
    public void Kapsamsiz_aktor_bos_kume_alir()
    {
        var scope = new InstitutionVisibility(Unrestricted: false, PathPrefix: null, InstitutionId: Guid.Empty);

        UserScopePolicy.VisibleInstitutionIds(scope, []).ShouldBeEmpty();
    }

    [Fact]
    public void Kurum_kimligi_null_olan_kapsamsiz_aktor_de_bos_kume_alir()
    {
        var scope = new InstitutionVisibility(Unrestricted: false, PathPrefix: null, InstitutionId: null);

        UserScopePolicy.VisibleInstitutionIds(scope, []).ShouldBeEmpty();
    }

    /// <summary>
    /// <c>null</c> ile boş liste AYNI ŞEY DEĞİLDİR ve çağıran ikisini karıştırırsa sonuç ters
    /// döner: <c>null</c> "süzme", boş liste "yalnız bağsızları göster" demektir.
    /// </summary>
    [Fact]
    public void Null_ile_bos_liste_ayni_sey_degildir()
    {
        var platform = new InstitutionVisibility(Unrestricted: true, PathPrefix: null, InstitutionId: null);
        var kapsamsiz = new InstitutionVisibility(Unrestricted: false, PathPrefix: null, InstitutionId: Guid.Empty);

        UserScopePolicy.VisibleInstitutionIds(platform, []).ShouldBeNull();
        UserScopePolicy.VisibleInstitutionIds(kapsamsiz, []).ShouldNotBeNull();
    }
}
