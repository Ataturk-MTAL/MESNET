using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// <b>Ölçülmüş sessiz hata — iki kontrol noktasının ayrışması (B parçası, canlı, 30.08.2026).</b>
///
/// <para><b>Ölçülen durum:</b> platform yetkili aktör (<c>admin</c>, ev kurumu Mersin İl
/// MEM) alt ağacı dışındaki bir okula (Gazi MTAL) geçti. <c>SetActiveInstitutionHandler</c>
/// (değiştirme anı) platform muafiyetini kabul etti ve kaydı güncelledi
/// (<c>UserAccount.ActiveInstitutionId</c> saklandı) — ama <c>PermissionClaimsTransformation.
/// EnrichActiveContextClaimAsync</c> (her çözümleme) muafiyeti almıyordu:
/// <c>ActiveContextPolicy.Resolve</c> platformsuz bir aktörmüş gibi ağaç kontrolüne düşüp
/// reddediyor, <c>active_institution_id</c> claim'i hiç doğmuyordu. Sonuç: kullanıcı hata
/// görmedi, geçiş "başarılı" göründü, <c>/auth/me</c> hâlâ <c>None</c> döndürdü, arayüz ev
/// kurumunda kaldı (0 öğrenci).</para>
///
/// <para><b>Neden kaynak taraması:</b> <c>PermissionClaimsTransformation</c> Marten/HTTP
/// context'e bağlı, depoda mocking kütüphanesi yok, entegrasyon testleri bu ortamda
/// koşmuyor. Dikiş bir ETKİLEŞİM iddiasıdır (doğru argümanla çağırma), saf fonksiyona
/// çıkarılamaz. Depo idiomu:
/// <c>ChangeUserInstitutionPlatformScopeDriftTests</c>,
/// <c>TenantResolutionMiddlewareArgumentDriftTests</c>.</para>
/// </summary>
public sealed class EnrichActiveContextClaimPlatformScopeDriftTests
{
    /// <summary>
    /// Çağrı sitesini (<c>await EnrichActiveContextClaimAsync(...)</c>) yakalar — metod
    /// TANIMI (<c>private async Task EnrichActiveContextClaimAsync(...)</c>) başında
    /// <c>await</c> taşımadığı için bu regex'le eşleşmez.
    /// </summary>
    private static readonly Regex EnrichCall = new(
        @"await\s+EnrichActiveContextClaimAsync\((?<args>[\s\S]*?)\);",
        RegexOptions.Compiled);

    [Fact]
    public void EnrichActiveContextClaimAsync_cagrisi_platform_muafiyetini_gecer()
    {
        var file = Path.Combine(
            RepoRoot(), "src", "MESNET.Common.Infrastructure", "Security",
            "PermissionClaimsTransformation.cs");

        File.Exists(file).ShouldBeTrue($"Dosya bulunamadı: {file}");

        var source = StripComments(File.ReadAllText(file));
        var match = EnrichCall.Match(source);

        match.Success.ShouldBeTrue(
            "PermissionClaimsTransformation.cs içinde 'await EnrichActiveContextClaimAsync(...)' "
            + "çağrısı bulunamadı. Aktif bağlam claim'i hiç çözülmez — B parçasının TAMAMI "
            + "işlevsiz kalır.");

        match.Groups["args"].Value.ShouldContain("HasPlatformScope(",
            customMessage:
                "EnrichActiveContextClaimAsync(...) çağrısı platform muafiyetini "
                + "(HasPlatformScope(entry, principal)) TAŞIMIYOR. Bu argüman silinirse ya da "
                + "sabit 'false' ile değiştirilirse SetActiveInstitutionHandler'ın kabul ettiği "
                + "platform geçişi her çözümlemede sessizce düşer: kayıt ActiveInstitutionId'yi "
                + "taşır, ama active_institution_id claim'i hiç doğmaz — kullanıcı hatasız "
                + "biçimde ev kurumunda kalmaya devam eder (canlıda ölçüldü, 30.08.2026).");
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
