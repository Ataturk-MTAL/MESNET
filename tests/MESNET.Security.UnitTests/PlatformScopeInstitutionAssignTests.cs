using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Platform muafiyetinin kullanıcı–kurum bağı değiştirmede gerçekten uygulandığını kilitler.
///
/// <para><b>Ölçülmüş hata (30.08.2026, canlı yığın):</b> <c>ChangeUserInstitutionHandler</c>
/// <c>CanAssign</c>'ı ÜÇ argümanla çağırıyordu, yani <c>hasPlatformScope</c> varsayılan
/// <c>false</c> kalıyordu. Sonuç: <c>platform:tenant:manage</c> taşıyan aktör bir kullanıcıyı
/// BAŞKA kuruma bağlayamıyordu — 422 <c>Security.ActiveContextOutOfScope</c>. Oysa ADR-0003'te
/// o iznin var olma sebebi tam olarak budur.</para>
///
/// <para><b>Neden politika seviyesinde test:</b> handler Marten'a doğrudan bağlı ve depoda
/// mocking kütüphanesi yok. Bu testler politikanın sözleşmesini kilitler; handler'ın o
/// sözleşmeyi doğru argümanla çağırdığını <c>ChangeUserInstitutionPlatformScopeDriftTests</c>
/// kaynak taramasıyla kilitler. İkisi birlikte dikişi kapatır.</para>
/// </summary>
public sealed class PlatformScopeInstitutionAssignTests
{
    private static readonly Guid AktorunKurumu = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid HedefKurum = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Platform_yetkisiyle_baska_kuruma_baglanabilir()
    {
        UserInstitutionScopePolicy.CanAssign(
            actorInstitutionId: AktorunKurumu,
            currentInstitutionId: AktorunKurumu,
            targetInstitutionId: HedefKurum,
            hasPlatformScope: true).ShouldBeTrue();
    }

    [Fact]
    public void Platform_yetkisi_YOKSA_baska_kuruma_baglanamaz()
    {
        // Muafiyetin gerçekten taşıyıcı olduğunun ölçümü: tek fark dördüncü argüman.
        UserInstitutionScopePolicy.CanAssign(
            actorInstitutionId: AktorunKurumu,
            currentInstitutionId: AktorunKurumu,
            targetInstitutionId: HedefKurum,
            hasPlatformScope: false).ShouldBeFalse();
    }

    [Fact]
    public void Argumansiz_cagri_muafiyeti_UYGULAMAZ()
    {
        // Varsayılan false'tur. Handler'ın dördüncü argümanı geçmeyi unutması tam olarak
        // ölçülen hataydı; bu test o varsayılanın sessiz olduğunu belgeler.
        UserInstitutionScopePolicy.CanAssign(AktorunKurumu, AktorunKurumu, HedefKurum)
            .ShouldBeFalse();
    }
}
