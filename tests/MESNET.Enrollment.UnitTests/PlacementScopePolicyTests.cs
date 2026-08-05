using MESNET.Enrollment.Core.Policies;
using Shouldly;
using Xunit;

namespace MESNET.Enrollment.UnitTests;

/// <summary>
/// Yerleştirme kapsam merdiveni (#184) — ADR-0001'in son kalan kod ihlalinin yerine geçen kural.
///
/// <para><b>Eski kod rol adına bakıyordu</b> ve o yüzden bakım borcu üretiyordu: #129'da müdür
/// yardımcısı ayrı role çıkınca elle <c>!IsInRole(DeputyDirector)</c> eklendi; #172'de
/// <c>CompanyHR</c> eklendiğinde <b>eklenmedi</b> ve işletme İK, işletme kapsamına hiç
/// giremiyordu. Rol adı organizasyon şemasının bugünkü fotoğrafıdır; şema kayar, kod kaymaz.</para>
///
/// <para>Yeni kural üç ayrı yetki kaynağına dayanır ve hiçbiri rol adı değildir:
/// geniş izin → işletme claim'i → koordinatör kaydı → <b>boş</b>.</para>
/// </summary>
public sealed class PlacementScopePolicyTests
{
    private static readonly Guid Kurum = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Isletme = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid BaskaIsletme = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Ogretmen = Guid.Parse("44444444-4444-4444-4444-444444444444");

    // ── 1. basamak: okul yönetimi ───────────────────────────────────────────────────────

    [Fact]
    public void Genis_izinli_kullanici_kurumun_tamamini_gorur()
    {
        var scope = PlacementScopePolicy.Resolve(
            hasInstitutionWideView: true, Kurum, businessIdClaim: null,
            businessIdFilter: null, coordinatorTeacherId: null).ShouldNotBeNull();

        scope.InstitutionId.ShouldBe(Kurum);
        scope.TeacherId.ShouldBeNull("Kurum geneli görüşte koordinatör daraltması olmamalı.");
        scope.BusinessId.ShouldBeNull();
    }

    [Fact]
    public void Genis_izinli_kullanici_kendi_isletme_filtresini_uygulayabilir()
    {
        var scope = PlacementScopePolicy.Resolve(
            hasInstitutionWideView: true, Kurum, businessIdClaim: null,
            businessIdFilter: Isletme, coordinatorTeacherId: null).ShouldNotBeNull();

        scope.BusinessId.ShouldBe(Isletme);
    }

    /// <summary>
    /// Geniş izin, öğretmen kaydı olsa bile kazanır — öğretmenliği de olan bir müdür yardımcısı
    /// kurum geneli görünürlüğünü kaybetmemeli. #129'da elle eklenen satırın koruduğu davranış.
    /// </summary>
    [Fact]
    public void Genis_izin_ogretmen_kaydindan_once_gelir()
    {
        var scope = PlacementScopePolicy.Resolve(
            hasInstitutionWideView: true, Kurum, businessIdClaim: null,
            businessIdFilter: null, coordinatorTeacherId: Ogretmen).ShouldNotBeNull();

        scope.TeacherId.ShouldBeNull();
    }

    // ── 2. basamak: işletme ─────────────────────────────────────────────────────────────

    /// <summary>
    /// <b>Asıl kazanım:</b> kapsam claim'den okunur. İşletme İK gibi <i>sonradan eklenen</i>
    /// roller, <c>business_id</c> taşıdıkları anda doğru kapsama kendiliğinden girer — kimsenin
    /// bu dosyaya rol adı eklemesi gerekmez.
    /// </summary>
    [Fact]
    public void Isletme_claimi_olan_kullanici_rol_adina_bakilmadan_kendi_isletmesini_gorur()
    {
        var scope = PlacementScopePolicy.Resolve(
            hasInstitutionWideView: false, Kurum, businessIdClaim: Isletme,
            businessIdFilter: null, coordinatorTeacherId: null).ShouldNotBeNull();

        scope.BusinessId.ShouldBe(Isletme);
    }

    /// <summary>İstekten gelen filtre claim'i EZEMEZ — başka işletmenin verisi istenemez.</summary>
    [Fact]
    public void Isletme_claimi_kullanicinin_filtresini_ezer()
    {
        var scope = PlacementScopePolicy.Resolve(
            hasInstitutionWideView: false, Kurum, businessIdClaim: Isletme,
            businessIdFilter: BaskaIsletme, coordinatorTeacherId: null).ShouldNotBeNull();

        scope.BusinessId.ShouldBe(Isletme, "Kapsam istekten değil claim'den okunur.");
    }

    [Fact]
    public void Bos_isletme_claimi_kapsam_saglamaz()
    {
        PlacementScopePolicy.Resolve(
            hasInstitutionWideView: false, Kurum, businessIdClaim: Guid.Empty,
            businessIdFilter: null, coordinatorTeacherId: null).ShouldBeNull();
    }

    // ── 3. basamak: koordinatör öğretmen ────────────────────────────────────────────────

    [Fact]
    public void Ogretmen_yalniz_koordine_ettigi_yerlestirmeleri_gorur()
    {
        var scope = PlacementScopePolicy.Resolve(
            hasInstitutionWideView: false, Kurum, businessIdClaim: null,
            businessIdFilter: null, coordinatorTeacherId: Ogretmen).ShouldNotBeNull();

        scope.TeacherId.ShouldBe(Ogretmen);
        scope.InstitutionId.ShouldBe(Kurum);
    }

    [Fact]
    public void Isletme_claimi_ogretmen_kaydindan_once_gelir()
    {
        var scope = PlacementScopePolicy.Resolve(
            hasInstitutionWideView: false, Kurum, businessIdClaim: Isletme,
            businessIdFilter: null, coordinatorTeacherId: Ogretmen).ShouldNotBeNull();

        scope.BusinessId.ShouldBe(Isletme);
        scope.TeacherId.ShouldBeNull();
    }

    // ── 4. basamak: çözülemeyen kapsam ──────────────────────────────────────────────────

    /// <summary>
    /// Kapsamı çözülemeyen kullanıcı <b>boş</b> görür. Sessizce kurum geneline düşmek, kapsamı
    /// belirsiz bir kullanıcıya her şeyi göstermek olurdu.
    /// </summary>
    [Fact]
    public void Hicbir_kapsam_cozulemezse_bos_doner()
    {
        PlacementScopePolicy.Resolve(
            hasInstitutionWideView: false, Kurum, businessIdClaim: null,
            businessIdFilter: null, coordinatorTeacherId: null).ShouldBeNull();
    }

    /// <summary>Kullanıcının kendi filtresi, kapsamı olmayana kapsam KAZANDIRMAZ.</summary>
    [Fact]
    public void Kapsamsiz_kullanici_filtre_gondererek_kapsam_kazanamaz()
    {
        PlacementScopePolicy.Resolve(
            hasInstitutionWideView: false, Kurum, businessIdClaim: null,
            businessIdFilter: Isletme, coordinatorTeacherId: null).ShouldBeNull();
    }
}
