using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Hangi okulun verisine dokunulabileceği kararı (ADR-0003 adım 6).
///
/// <para><b>Neden gerekti — ölçüldü.</b> İki okullu dev ortamında bu kontrol yokken B okulunun
/// müdürü A okulunun kaydını <b>okudu</b> (200, 7 kişilik personel listesiyle), <b>adını
/// değiştirdi</b> (200) ve personel listesine <b>kayıt ekledi</b> (201). Marten conjoined
/// kiracılığı bunu engelleyemez: <c>Institution</c> belgesi kiracının kendisidir ve kiracı
/// damgası taşımaz.</para>
/// </summary>
public sealed class InstitutionScopePolicyTests
{
    private static readonly Guid OkulA = Guid.Parse("efd57b88-2f47-471c-9f51-476f80fabfca");
    private static readonly Guid OkulB = Guid.Parse("a24ebbab-8c58-4373-b936-640fa3247e77");

    [Fact]
    public void Kendi_kurumuna_erisebilir()
    {
        InstitutionScopePolicy.CanAccess(OkulA, OkulA, hasPlatformScope: false).ShouldBeTrue();
    }

    [Fact]
    public void Baska_kuruma_erisemez()
    {
        InstitutionScopePolicy.CanAccess(OkulA, OkulB, hasPlatformScope: false).ShouldBeFalse(
            "Okul müdürü diğer okulun kaydına dokunamamalı — ölçülen sızıntı buydu.");
    }

    /// <summary>
    /// Kapsamsızlık <b>sınırsızlık değildir</b>. Kurumu olmayan kullanıcı (henüz bağlanmamış
    /// hesap) hiçbir kurumun verisine erişemez.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public void Kapsamsiz_aktor_hicbir_kuruma_erisemez(string? actorId)
    {
        Guid? actor = actorId is null ? null : Guid.Parse(actorId);

        InstitutionScopePolicy.CanAccess(actor, OkulA, hasPlatformScope: false).ShouldBeFalse();
    }

    /// <summary>
    /// Kurum üstü aktör (yeni okul açan) her kuruma erişir. Muafiyet <b>önce</b> bakılır;
    /// aksi hâlde kendi kurumu olmadığı için ilk kuralda elenirdi.
    /// </summary>
    [Fact]
    public void Platform_aktoru_her_kuruma_erisir()
    {
        InstitutionScopePolicy.CanAccess(null, OkulB, hasPlatformScope: true).ShouldBeTrue();
        InstitutionScopePolicy.CanAccess(OkulA, OkulB, hasPlatformScope: true).ShouldBeTrue();
    }

    /// <summary>Boş hedef bir kurum değildir; istekte alan boş bırakılarak kontrol atlatılamaz.</summary>
    [Fact]
    public void Bos_hedef_kabul_edilmez()
    {
        InstitutionScopePolicy.CanAccess(OkulA, Guid.Empty, hasPlatformScope: false).ShouldBeFalse();
    }

    [Fact]
    public void Listeleme_kendi_kurumuna_daralir()
    {
        InstitutionScopePolicy.VisibleInstitutionFilter(OkulA, hasPlatformScope: false)
            .ShouldBe(OkulA);
    }

    [Fact]
    public void Platform_aktorunun_listesi_daraltilmaz()
    {
        InstitutionScopePolicy.VisibleInstitutionFilter(null, hasPlatformScope: true)
            .ShouldBeNull();
    }

    /// <summary>
    /// Kapsamsız aktör <b>boş liste</b> görür, her şeyi değil. Süzgeç <c>null</c> dönseydi
    /// "daraltma yok" anlamına gelir ve bütün okulları açardı — sessiz sızıntının kılık
    /// değiştirmiş hâli.
    /// </summary>
    [Fact]
    public void Kapsamsiz_aktor_bos_liste_gorur()
    {
        InstitutionScopePolicy.VisibleInstitutionFilter(null, hasPlatformScope: false)
            .ShouldBe(Guid.Empty);
    }
}
