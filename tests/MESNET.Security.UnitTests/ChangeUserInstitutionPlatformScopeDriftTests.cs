using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// <c>ChangeUserInstitution</c> handler'ının <c>CanAssign</c>'a platform muafiyetini GEÇTİĞİNİ
/// kilitler.
///
/// <para><b>Neden kaynak taraması:</b> handler Marten'a doğrudan bağlı, depoda mocking
/// kütüphanesi yok ve entegrasyon testleri bu ortamda koşmuyor. Dikiş bir ETKİLEŞİM iddiasıdır
/// (doğru argümanla çağırma), saf fonksiyona çıkarılamaz. Depo idiomu: <c>InstitutionScopeDriftTests</c>,
/// <c>TenantlessSessionDriftTests</c>, <c>TenantResolutionMiddlewareArgumentDriftTests</c>.</para>
///
/// <para><b>Ölçülmüş hata:</b> argüman yokken <c>platform:tenant:manage</c> taşıyan aktör
/// kullanıcıyı başka kuruma bağlayamıyordu (canlı yığın, 30.08.2026).</para>
/// </summary>
public sealed class ChangeUserInstitutionPlatformScopeDriftTests
{
    [Fact]
    public void ChangeUserInstitution_CanAssign_cagrisi_platform_muafiyetini_gecer()
    {
        var kaynak = File.ReadAllText(HandlerYolu());

        // ChangeUserInstitution dalındaki çağrı: dördüncü argüman Platform.TenantManage olmalı.
        var cagri = Regex.Match(
            kaynak,
            @"var\s+normalYol\s*=.*?CanAssign\((?<args>.*?)\);",
            RegexOptions.Singleline);

        cagri.Success.ShouldBeTrue(
            "ChangeUserInstitution handler'ında 'normalYol = ... CanAssign(...)' çağrısı bulunamadı.");

        cagri.Groups["args"].Value.Contains("Permissions.Platform.TenantManage", StringComparison.Ordinal)
            .ShouldBeTrue(
                "CanAssign çağrısı platform muafiyetini GEÇMİYOR. Argüman yoksa varsayılan false olur "
                + "ve platform:tenant:manage taşıyan aktör kullanıcıyı başka kuruma bağlayamaz "
                + "(ADR-0003'te o iznin var olma sebebi budur).");
    }

    private static string HandlerYolu()
    {
        var dizin = new DirectoryInfo(AppContext.BaseDirectory);
        while (dizin is not null && !Directory.Exists(Path.Combine(dizin.FullName, "src")))
            dizin = dizin.Parent;

        dizin.ShouldNotBeNull("Depo kökü bulunamadı.");

        return Path.Combine(dizin!.FullName, "src", "Modules", "Security",
            "MESNET.Security.Application", "Handlers", "UserManagementHandler.cs");
    }
}
