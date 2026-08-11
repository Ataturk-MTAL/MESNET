using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Anonim uçların listesi <b>bilinçli olarak kapalıdır</b> (#149).
///
/// <para><b>Neden kilit gerekiyor:</b> kimliği doğrulanmamış istek hiçbir okula ait olamaz,
/// bu yüzden <c>TenantResolutionMiddleware</c> onu <b>platform</b> kiracısında çalıştırır.
/// Bu, yalnızca anonim çağıranın dokunabildiği belgeler <i>kimlik katmanında</i> olduğu için
/// güvenlidir — o belgeler kiracı damgası taşımaz, hangi kiracıyla okundukları sonucu
/// değiştirmez.</para>
///
/// <para><b>Kiracıya ait bir belgeye dokunan yeni bir anonim uç eklenirse</b> sonuç sessizdir:
/// istisna yok, hata yok, yalnız <b>boş sonuç</b> — çünkü okul verisi platform kiracısında
/// görünmez. Yazma tarafı daha kötüdür: satır platform damgasıyla doğar ve onu yazan okul bile
/// bir daha göremez. Derleyici bunu göremez; tek savunma listenin kapalı kalmasıdır.</para>
///
/// <para>Yeni bir anonim uç gerçekten gerekiyorsa: dokunduğu <b>her</b> belgenin
/// <c>DocumentTenancyMap</c>'te <c>Identity</c> ya da <c>Shared</c> olduğunu doğrulayın, sonra
/// aşağıdaki listeye ekleyin. Ekleme, kararın verildiğinin kaydıdır.</para>
/// </summary>
public sealed class AnonymousEndpointDriftTests
{
    /// <summary>
    /// İzin verilen anonim uçlar: <c>dosya:uç</c>. Davet tamamlama, kimlik katmanının
    /// onboarding kenarıdır — daveti tamamlayan kişinin henüz kullanıcı kaydı, dolayısıyla
    /// kiracısı yoktur. Dokunduğu iki belge de kimlik katmanındadır
    /// (<c>UserInvitation</c>, <c>UserAccount</c>).
    /// </summary>
    private static readonly HashSet<string> Allowed = new(StringComparer.Ordinal)
    {
        "Modules/Security/MESNET.Security.Api/InvitationEndpoints.cs:/{invitationId:guid}/complete",
    };

    private static readonly Regex AnonymousMapping = new(
        @"Map(?:Get|Post|Put|Patch|Delete)\(\s*""(?<route>[^""]*)""[^;]*?AllowAnonymous\(\)",
        RegexOptions.Compiled | RegexOptions.Singleline);

    [Fact]
    public void Anonim_uc_listesi_kapalidir()
    {
        var found = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in SourceFiles())
        {
            var text = File.ReadAllText(file);
            if (!text.Contains("AllowAnonymous", StringComparison.Ordinal))
                continue;

            foreach (Match match in AnonymousMapping.Matches(text))
                found.Add($"{Relative(file)}:{match.Groups["route"].Value}");
        }

        found.Except(Allowed).ShouldBeEmpty(
            "Yeni anonim uç eklenmiş. Anonim istek PLATFORM kiracısında çalışır; kiracıya ait "
            + "belgeye dokunursa sonuç sessizce boş döner, yazma yaparsa satır platform "
            + "damgasıyla doğar ve onu yazan okul bir daha göremez. Dokunduğu her belgenin "
            + "DocumentTenancyMap'te Identity ya da Shared olduğunu doğrulayıp bu listeye ekleyin.");

        Allowed.Except(found).ShouldBeEmpty(
            "Listede olup kaynakta bulunmayan anonim uç var — uç kaldırıldıysa listeden de "
            + "çıkarın; yoksa liste zamanla anlamını yitirir.");
    }

    private static IEnumerable<string> SourceFiles()
    {
        var root = Path.Combine(RepoRoot(), "src");
        var obj = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        var bin = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(obj, StringComparison.Ordinal)
                     && !f.Contains(bin, StringComparison.Ordinal));
    }

    private static string Relative(string file) =>
        Path.GetRelativePath(Path.Combine(RepoRoot(), "src"), file).Replace('\\', '/');

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
