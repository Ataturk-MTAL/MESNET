using MESNET.Common.Shared.Tenancy;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Aktif bağlamın kiracı çözümlemesindeki yeri (B parçası).
///
/// <para><b>Aktif bağlam kiracıyı DEĞİŞTİRİR</b> — okul verisi okulun kiracısındadır ve il
/// yetkilisi oraya ancak o kiracıda çalışarak ulaşır. Ama <c>institution_id</c> claim'i
/// (ev kurumu) DEĞİŞMEZ; denetim izinin "kim olduğun / nerede davrandığın" ayrımı ona
/// bağlıdır.</para>
///
/// <para>Bu fonksiyona ulaşan aktif bağlam <b>zaten doğrulanmıştır</b>
/// (<c>ActiveContextPolicy</c>): geçersiz bağlam claim'e hiç dönüşmez. Buradaki iş yalnız
/// tercih sırasıdır.</para>
/// </summary>
public sealed class TenantResolutionActiveContextTests
{
    private static readonly Guid EvKurumu = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Okul = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly string[] IzinYok = [];
    private static readonly string[] PlatformIzni = ["platform:tenant:manage"];

    [Fact]
    public void Aktif_baglam_varsa_kiraci_odur()
    {
        TenantResolution.Resolve(EvKurumu, IzinYok, Okul)
            .ShouldBe(TenantResolution.ForInstitution(Okul));
    }

    [Fact]
    public void Aktif_baglam_yoksa_ev_kurumu_kiraci_olur()
    {
        TenantResolution.Resolve(EvKurumu, IzinYok, null)
            .ShouldBe(TenantResolution.ForInstitution(EvKurumu));
    }

    [Fact]
    public void Bos_Guid_aktif_baglam_yok_sayilir()
    {
        TenantResolution.Resolve(EvKurumu, IzinYok, Guid.Empty)
            .ShouldBe(TenantResolution.ForInstitution(EvKurumu));
    }

    [Fact]
    public void Kurumu_olmayan_platform_aktoru_baglam_secebilir()
    {
        // Platform aktörünün ev kurumu yoktur; bağlam seçtiğinde o okulda çalışır.
        TenantResolution.Resolve(null, PlatformIzni, Okul)
            .ShouldBe(TenantResolution.ForInstitution(Okul));
    }

    [Fact]
    public void Baglamsiz_platform_aktoru_platform_kiracisinda_kalir()
    {
        TenantResolution.Resolve(null, PlatformIzni, null)
            .ShouldBe(TenantResolution.Platform);
    }

    [Fact]
    public void Kapsamsiz_kullanici_baglamsizken_kiracisiz_kalir()
    {
        // Kapsamsız kullanıcıya kiracı UYDURULMAZ (ADR-0003) — bu davranış değişmedi.
        TenantResolution.Resolve(null, IzinYok, null).ShouldBeNull();
    }

    [Fact]
    public void Eski_iki_parametreli_cagri_davranisi_korunur()
    {
        // Depoda bu fonksiyonun mevcut çağrıları var; üçüncü parametre isteğe bağlıdır.
        TenantResolution.Resolve(EvKurumu, IzinYok)
            .ShouldBe(TenantResolution.ForInstitution(EvKurumu));
    }
}
