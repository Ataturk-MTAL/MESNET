using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Kimlikle yüklenen TEK kaydın kapsam kararı (#284).
///
/// <para>Liste sorgusu <c>Where</c>'den geçer; <c>LoadAsync&lt;T&gt;(id)</c> geçmez. Kimlikle
/// çekmek "zaten kapsamlı" anlamına gelmez — tanımlayıcının tahmin edilemezliğine dayanmak
/// yetkilendirme değildir. Ölçüldü: üç davet yazma ucu tam olarak bunu yapıyordu ve başka
/// okulun daveti onaylanabiliyordu.</para>
/// </summary>
public class UserScopeVisibilityTests
{
    private static readonly Guid OkulA = Guid.NewGuid();
    private static readonly Guid OkulB = Guid.NewGuid();

    [Fact]
    public void Platform_aktoru_her_kaydi_gorur()
    {
        // null = süzgeç UYGULANMAZ. Boş listeyle karıştırılırsa sonuç TERS döner.
        UserScopePolicy.IsVisible(null, OkulB).ShouldBeTrue();
    }

    [Fact]
    public void Kendi_kurumunun_kaydi_gorunur()
    {
        UserScopePolicy.IsVisible([OkulA], OkulA).ShouldBeTrue();
    }

    [Fact]
    public void BASKA_kurumun_kaydi_GORUNMEZ()
    {
        // KİLİT NOKTA — #284'ün ta kendisi.
        UserScopePolicy.IsVisible([OkulA], OkulB).ShouldBeFalse();
    }

    [Fact]
    public void Kurum_bagi_OLMAYAN_kayit_gorunur_kalir()
    {
        // Okuma tarafındaki kararla aynı: aksi hâlde kapsamsız davet hiç kimse tarafından
        // onaylanamaz/reddedilemez hâle gelir ve sonsuza kadar beklemede kalırdı.
        UserScopePolicy.IsVisible([OkulA], null).ShouldBeTrue();
    }

    [Fact]
    public void Kapsamsiz_aktor_yalniz_bagsiz_kaydi_gorur()
    {
        // Boş liste = "yalnız kurum bağı olmayanlar". Her şeyi görmek yerine hiçbir şey görmek.
        UserScopePolicy.IsVisible([], OkulA).ShouldBeFalse();
        UserScopePolicy.IsVisible([], null).ShouldBeTrue();
    }
}
