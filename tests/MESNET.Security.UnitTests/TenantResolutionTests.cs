using MESNET.Common.Shared.Tenancy;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// İsteğin hangi kiracı adına çalışacağı (#149).
///
/// <para><b>En önemli iddia: kiracı UYDURULMAZ.</b> Kapsamsız kullanıcıyı varsayılan ya da
/// platform kiracısına düşürmek, onun yazmalarını sessizce yanlış bölmeye göndermek olurdu —
/// kiracılığın engellemek için var olduğu hatanın ta kendisi. Çözülemeyen kiracı <c>null</c>
/// döner ve erişim gürültülü biçimde başarısız olur.</para>
/// </summary>
public sealed class TenantResolutionTests
{
    private static readonly Guid Institution = Guid.Parse("efd57b88-2f47-471c-9f51-476f80fabfca");

    [Fact]
    public void Kurumu_olan_kullanicinin_kiracisi_kendi_kurumudur()
    {
        TenantResolution.Resolve(Institution, ["institution:view"])
            .ShouldBe(Institution.ToString());
    }

    [Fact]
    public void Kapsamsiz_kullaniciya_kiraci_uydurulmaz()
    {
        TenantResolution.Resolve(null, ["institution:view"]).ShouldBeNull();
    }

    /// <summary>Boş Guid de kapsamsızlıktır — "kurum yok" ile aynı anlama gelir.</summary>
    [Fact]
    public void Bos_guid_kapsam_sayilmaz()
    {
        TenantResolution.Resolve(Guid.Empty, []).ShouldBeNull();
    }

    /// <summary>
    /// Kurum üstü katmanda çalışan kullanıcı (bugün <c>SystemAdmin</c>) hiçbir okula bağlı
    /// değildir ama ulusal parametreleri yazar; kiracısı <c>platform</c>'dur.
    /// </summary>
    [Fact]
    public void Kurum_ustu_yetkili_platform_kiracisinda_calisir()
    {
        TenantResolution.Resolve(null, ["platform:parameter:manage", "salary:parameter:view"])
            .ShouldBe(TenantResolution.Platform);
    }

    /// <summary>
    /// <b>Sıra önemli.</b> Kurumu olan kullanıcı, platform izni de taşısa kendi okulunda kalır —
    /// aksi hâlde okul müdürü ulusal katmana yazarken okul verisinden kopardı. Dev ortamındaki
    /// <c>admin</c> hesabı tam olarak bu durumdadır: hem SystemAdmin hem InstitutionManager.
    /// </summary>
    [Fact]
    public void Kurumu_olan_platform_yetkilisi_kendi_okulunda_kalir()
    {
        TenantResolution.Resolve(Institution, ["platform:parameter:manage", "institution:manage"])
            .ShouldBe(Institution.ToString());
    }

    [Fact]
    public void Platform_kiracisi_bir_guid_degildir()
    {
        Guid.TryParse(TenantResolution.Platform, out _).ShouldBeFalse(
            "Platform kiracısı bir okul kimliğiyle karışmamalı; adı olmalı.");
    }
}
