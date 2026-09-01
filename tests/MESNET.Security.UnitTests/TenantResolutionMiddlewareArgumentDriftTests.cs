using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// <b>Ölçülmüş boş kilit — middleware dikişi (B parçası).</b>
///
/// <para><b>Neden bu test var — ölçüldü:</b> <c>TenantResolutionMiddleware.InvokeAsync</c>
/// içindeki <c>TenantResolution.Resolve(...)</c> çağrısından
/// <c>ActiveInstitutionIdOf(context.User)</c> argümanı silindiğinde B parçasının TAMAMI
/// işlevsiz kalıyor — il yetkilisi bir okula geçse bile <see cref="MESNET.Common.Shared.Tenancy.TenantResolution.Resolve"/>
/// aktif bağlamı hiç göremiyor, kiracı her zaman ev kurumuna (ya da kapsamsızsa hiçbir
/// şeye) çözülüyor, dolayısıyla il yetkilisi bağlamdaki okulun HİÇBİR verisini görmüyor —
/// ama çözüm geneli <b>1747/1747 yeşil</b> kalıyor. Derleyici argümanı azaltılmış bir çağrıyı
/// reddetmez (metodun fazladan parametresi optional değilse derleme hatası verirdi, ama
/// mevcut imzada üçüncü parametre <c>Guid?</c> olduğu için <c>null</c> ya da eksik argüman
/// derlenir — üstelik regresyon burada üçüncü argümanı SİLMEK değil, İÇİNİ boşaltmak
/// (<c>null</c> literal) şeklinde de olabilir).</para>
///
/// <para><b>Neden davranışsal test yerine kaynak taraması:</b> <c>InvokeAsync</c>
/// <c>HttpContext</c> gerektirir ve middleware'i uçtan uca koşturmak entegrasyon testi
/// gerektirir (Postgres, Keycloak) — bu depoda birim test katmanının kapsamı dışında.
/// Depo idiomu (<c>InstitutionClaimAuthorityTests</c>, <c>TenantlessSessionDriftTests</c>,
/// <c>AnonymousEndpointDriftTests</c>) aynı sınırda kaynak taramasına düşer: dikiş TEK bir
/// çağrı sitesidir, tarama sahtekarlığa yer bırakmaz.</para>
/// </summary>
public sealed class TenantResolutionMiddlewareArgumentDriftTests
{
    /// <summary>
    /// <c>TenantResolution.Resolve(...)</c> çağrısının GÖVDESİNİ yakalar — metod TANIMI
    /// (<c>Tenancy/TenantResolution.cs</c> içindeki <c>public static string? Resolve(...)</c>)
    /// bu regex'le eşleşmez, çünkü orada çağrı değil bildirim vardır (<c>Resolve(</c> sonrası
    /// parametre TİPLERİ gelir, argüman DEĞERLERİ değil). Bu test yalnız çağrı sitesindeki
    /// dosyayı (<c>TenantResolutionMiddleware.cs</c>) tarar.
    /// </summary>
    private static readonly Regex ResolveCall = new(
        @"TenantResolution\.Resolve\(([\s\S]*?)\);",
        RegexOptions.Compiled);

    [Fact]
    public void TenantResolution_Resolve_cagrisi_aktif_baglam_argumanini_tasir()
    {
        var file = Path.Combine(
            RepoRoot(), "src", "MESNET.Common.Infrastructure", "Tenancy",
            "TenantResolutionMiddleware.cs");

        File.Exists(file).ShouldBeTrue($"Dosya bulunamadı: {file}");

        var source = StripComments(File.ReadAllText(file));
        var match = ResolveCall.Match(source);

        match.Success.ShouldBeTrue(
            "TenantResolutionMiddleware.cs içinde TenantResolution.Resolve(...) çağrısı "
            + "bulunamadı. Kiracı hiç çözülemez — bütün istekler kiracısız kalır.");

        match.Groups[1].Value.ShouldContain("ActiveInstitutionIdOf(context.User)",
            customMessage:
                "TenantResolution.Resolve(...) çağrısı aktif bağlam argümanını "
                + "(ActiveInstitutionIdOf(context.User)) TAŞIMIYOR. Bu argüman silinirse ya "
                + "da null'la değiştirilirse B parçasının TAMAMI işlevsiz kalır: il "
                + "yetkilisi bir okula geçse bile kiracı her zaman ev kurumuna çözülür, "
                + "bağlamdaki okulun hiçbir verisi görünmez — derleme ve testler yeşil "
                + "kalırken (1747/1747) çalışma zamanında sessizce bozulur.");
    }

    private static string StripComments(string source)
    {
        var withoutBlocks = Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//.*$", string.Empty, RegexOptions.Multiline);
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MESNET.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Depo kökü bulunamadı (MESNET.slnx aranıyordu): {AppContext.BaseDirectory}");
    }
}
