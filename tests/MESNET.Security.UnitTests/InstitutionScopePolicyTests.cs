using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Hangi kurumun verisine dokunulabileceği kararı (ADR-0003 adım 6 + kurum hiyerarşisi).
///
/// <para><b>Neden gerekti — ölçüldü.</b> İki okullu dev ortamında bu kontrol yokken B okulunun
/// müdürü A okulunun kaydını <b>okudu</b> (200, 7 kişilik personel listesiyle), <b>adını
/// değiştirdi</b> (200) ve personel listesine <b>kayıt ekledi</b> (201). Marten conjoined
/// kiracılığı bunu engelleyemez: <c>Institution</c> belgesi kiracının kendisidir ve kiracı
/// damgası taşımaz.</para>
///
/// <para><b>Karar neden üç sonuçlu:</b> yol karşılaştırması hedefin kaydını okumayı gerektirir.
/// Kararı "izin var / yok / yola bakmak gerekiyor" diye ayırmak, okul kullanıcısının (aktör ==
/// hedef) hiçbir isteğinde ek okuma yapılmamasını sağlar — ek maliyet yalnız yeni üst-düğüm
/// yeteneği kullanıldığında ödenir.</para>
/// </summary>
public sealed class InstitutionScopePolicyTests
{
    private static readonly Guid OkulA = Guid.Parse("efd57b88-2f47-471c-9f51-476f80fabfca");
    private static readonly Guid OkulB = Guid.Parse("a24ebbab-8c58-4373-b936-640fa3247e77");

    // ── Decide: kimlik aşaması ──

    [Fact]
    public void Kendi_kurumuna_erisir_ve_yol_okumasi_gerekmez()
    {
        InstitutionScopePolicy.Decide(OkulA, OkulA, hasPlatformScope: false)
            .ShouldBe(InstitutionScopeOutcome.Allowed);
    }

    [Fact]
    public void Baska_kurum_yol_kontrolune_dusurulur()
    {
        InstitutionScopePolicy.Decide(OkulA, OkulB, hasPlatformScope: false)
            .ShouldBe(InstitutionScopeOutcome.NeedsPathCheck);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Kapsamsiz_aktor_hicbir_kuruma_erisemez(string? actorId)
    {
        Guid? actor = actorId is null ? null : Guid.Parse(actorId);

        InstitutionScopePolicy.Decide(actor, OkulA, hasPlatformScope: false)
            .ShouldBe(InstitutionScopeOutcome.Denied);
    }

    /// <summary>Muafiyet ÖNCE bakılır; platform aktörünün kendi kurumu yoktur.</summary>
    [Fact]
    public void Kurum_ustu_aktor_her_kuruma_erisir()
    {
        InstitutionScopePolicy.Decide(null, OkulB, hasPlatformScope: true)
            .ShouldBe(InstitutionScopeOutcome.Allowed);
        InstitutionScopePolicy.Decide(OkulA, OkulB, hasPlatformScope: true)
            .ShouldBe(InstitutionScopeOutcome.Allowed);
    }

    [Fact]
    public void Bos_hedef_reddedilir()
    {
        InstitutionScopePolicy.Decide(OkulA, Guid.Empty, hasPlatformScope: false)
            .ShouldBe(InstitutionScopeOutcome.Denied);
    }

    // ── CanAccessByPath: ağaç aşaması ──

    [Fact]
    public void Il_yetkilisi_kendi_ilindeki_okulu_gorur()
    {
        InstitutionScopePolicy.CanAccessByPath("/il/", "/il/ilce/okul/").ShouldBeTrue();
    }

    [Fact]
    public void Il_yetkilisi_baska_ilin_okulunu_goremez()
    {
        InstitutionScopePolicy.CanAccessByPath("/il/", "/baskail/ilce/okul/").ShouldBeFalse();
    }

    [Fact]
    public void Okul_ust_dugumu_goremez()
    {
        InstitutionScopePolicy.CanAccessByPath("/il/ilce/okul/", "/il/ilce/").ShouldBeFalse();
    }

    [Fact]
    public void Yolu_olmayan_aktor_yol_kontrolunu_gecemez()
    {
        InstitutionScopePolicy.CanAccessByPath(null, "/il/ilce/okul/").ShouldBeFalse();
    }

    // ── VisibleScope: listeleme daraltması ──

    [Fact]
    public void Kurum_ustu_aktorun_listesi_daraltilmaz()
    {
        var scope = InstitutionScopePolicy.VisibleScope(null, null, hasPlatformScope: true);

        scope.Unrestricted.ShouldBeTrue();
        scope.PathPrefix.ShouldBeNull();
        scope.InstitutionId.ShouldBeNull();
    }

    [Fact]
    public void Yolu_olan_aktorun_listesi_alt_agaca_daraltilir()
    {
        var scope = InstitutionScopePolicy.VisibleScope(OkulA, "/il/", hasPlatformScope: false);

        scope.Unrestricted.ShouldBeFalse();
        scope.PathPrefix.ShouldBe("/il/");
        scope.InstitutionId.ShouldBeNull();
    }

    /// <summary>
    /// <b>Geçiş ucu koşturulmadan yapılan dağıtım kurum sayfasını KIRMAMALI.</b> Yolu olmayan
    /// aktör bugünkü davranışı korur: yalnız kendi kurumunu görür. Bu bir genişletme değildir —
    /// yolu olmayan aktör hiçbir şey KAZANMAZ, yalnız sahip olduğunu kaybetmez.
    /// </summary>
    [Fact]
    public void Yolu_olmayan_aktor_yalniz_kendi_kurumunu_gorur()
    {
        var scope = InstitutionScopePolicy.VisibleScope(OkulA, null, hasPlatformScope: false);

        scope.Unrestricted.ShouldBeFalse();
        scope.PathPrefix.ShouldBeNull();
        scope.InstitutionId.ShouldBe(OkulA);
    }

    /// <summary>Kapsamsızlık sınırsızlık değildir: hiçbir kurumla eşleşmeyen bir daraltma döner.</summary>
    [Fact]
    public void Kapsamsiz_aktorun_listesi_bos_gelir()
    {
        var scope = InstitutionScopePolicy.VisibleScope(null, null, hasPlatformScope: false);

        scope.Unrestricted.ShouldBeFalse();
        scope.PathPrefix.ShouldBeNull();
        scope.InstitutionId.ShouldBe(Guid.Empty);
    }
}
