using MESNET.Security.Application.Handlers;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// <see cref="SetActiveInstitutionHandler.CanSwitchTo"/> — bağlam değiştirme ucunun kapsam
/// kararı (madde 4, canlı ortamda ölçülen tutarsızlık).
///
/// <para><b>Bulgu:</b> <c>platform:tenant:manage</c> taşıyan aktörde seçim listesi
/// <c>InstitutionScopePolicy.VisibleScope</c> üzerinden <c>Unrestricted</c> döndüğü için tüm
/// okulları gösteriyordu, ama bu uç yalnız "kendi kurumu" veya "aktörün alt ağacı"nı kabul
/// ediyordu — <c>InstitutionScopePolicy.Decide</c>'ın zaten taşıdığı platform muafiyeti hiç
/// geçilmiyordu. Sonuç: ekranın sunduğu eylem sunucuda 422 ile reddediliyordu.</para>
///
/// <para><b>Tutarlılık:</b> <c>TenantResolutionActiveContextTests.
/// Kurumu_olmayan_platform_aktoru_baglam_secebilir</c> platform aktörünün alt ağaç dışı bir
/// okulu kiracı olarak seçebildiğini zaten kilitliyordu — çözümleme katmanı destekliyordu, bu
/// uç vermiyordu.</para>
/// </summary>
public sealed class SetActiveInstitutionScopeTests
{
    private static readonly Guid IlYetkilisi = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string IlYolu = "/il/";
    private const string KendiIlindekiOkulYolu = "/il/ilce/okul/";
    private const string BaskaIlinOkuluYolu = "/baska-il/ilce/okul/";
    private static readonly Guid BaskaIlinOkulu = Guid.Parse("a24ebbab-8c58-4373-b936-640fa3247e77");

    [Fact]
    public void Platform_yetkisi_olan_aktor_alt_agaci_disindaki_kuruma_gecebilir()
    {
        // Platform aktörünün kendi kurumu/yolu bile olmayabilir (ADR-0003 adım 6) — muafiyet
        // ağaç kontrolüne hiç girmeden Allowed vermeli.
        SetActiveInstitutionHandler.CanSwitchTo(
            actorInstitutionId: null,
            actorPath: null,
            targetInstitutionId: BaskaIlinOkulu,
            targetPath: BaskaIlinOkuluYolu,
            hasPlatformScope: true)
            .ShouldBeTrue();
    }

    [Fact]
    public void Platform_yetkisi_olan_il_yetkilisi_de_alt_agac_disina_gecebilir()
    {
        SetActiveInstitutionHandler.CanSwitchTo(
            actorInstitutionId: IlYetkilisi,
            actorPath: IlYolu,
            targetInstitutionId: BaskaIlinOkulu,
            targetPath: BaskaIlinOkuluYolu,
            hasPlatformScope: true)
            .ShouldBeTrue();
    }

    [Fact]
    public void Platform_yetkisi_OLMAYAN_aktor_alt_agaci_disindaki_kuruma_gecemez()
    {
        SetActiveInstitutionHandler.CanSwitchTo(
            actorInstitutionId: IlYetkilisi,
            actorPath: IlYolu,
            targetInstitutionId: BaskaIlinOkulu,
            targetPath: BaskaIlinOkuluYolu,
            hasPlatformScope: false)
            .ShouldBeFalse();
    }

    [Fact]
    public void Platform_yetkisi_OLMAYAN_aktor_kendi_alt_agacina_gecebilir()
    {
        // Regresyon koruması: muafiyet eklenirken il/ilçe yetkilisinin bugünkü hakkı bozulmamalı.
        SetActiveInstitutionHandler.CanSwitchTo(
            actorInstitutionId: IlYetkilisi,
            actorPath: IlYolu,
            targetInstitutionId: BaskaIlinOkulu,
            targetPath: KendiIlindekiOkulYolu,
            hasPlatformScope: false)
            .ShouldBeTrue();
    }

    [Fact]
    public void Kapsamsiz_ve_yetkisiz_aktor_hicbir_kuruma_gecemez()
    {
        SetActiveInstitutionHandler.CanSwitchTo(
            actorInstitutionId: null,
            actorPath: null,
            targetInstitutionId: BaskaIlinOkulu,
            targetPath: BaskaIlinOkuluYolu,
            hasPlatformScope: false)
            .ShouldBeFalse();
    }
}
