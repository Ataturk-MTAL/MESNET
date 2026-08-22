using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Yetki taşıyan bir alanı yazan her sınıf izin önbelleğini <b>temizlemek zorundadır</b> (#209).
///
/// <para><b>Yaşanan:</b> desen zaten vardı ve altı yerde doğru uygulanmıştı; <c>SyncUsersFromKeycloak</c>
/// atlanmıştı. Sonuç: senkronizasyon <c>UserAccount.Roles</c>'u günceller, ama izinler
/// <b>5 dakika daha eski hâliyle</b> verilir.</para>
///
/// <para>Ölçüldü — kayıt düzeltildikten sonra aynı uç, 30 saniyede bir:</para>
/// <code>
///  30s → 403    120s → 403
///  60s → 403    150s → 403
///  90s → 403    180s → 200
/// </code>
///
/// <para>Bu sessiz bir tuzak: yönetici düzeltmenin işe yaramadığını sanıp geri alabilir ya da
/// tekrar tekrar uygulayabilir. #205'te tam olarak bu yaşandı.</para>
///
/// <para><b>Neden liste değil tarama:</b> elle tutulan bir "şu handler'lar temizlemeli" listesi,
/// yeni eklenen handler'ı görmez — yani bu hatanın tekrarını engellemez. Tarama, yetki taşıyan
/// alanların <b>yazımını</b> arar; yeni handler kendiliğinden kapsama girer.</para>
/// </summary>
public sealed class PermissionCacheInvalidationDriftTests
{
    /// <summary>
    /// İzin ya da kapsam üreten alanlar. Biri yazılıyorsa önbellekteki girdi bayatlamıştır.
    ///
    /// <para>Nesne başlatıcı (<c>new UserAccount { Roles = ... }</c>) bilerek eşleşmez: yeni
    /// hesabın önbellekte girdisi olamaz. Kayıt bulunamayan kullanıcı için dönüşüm zaten
    /// hiçbir şey önbelleğe koymaz.</para>
    /// </summary>
    private static readonly Regex PermissionFieldWrite = new(
        @"\w+\.(Roles|IsEnabled|DirectPermissions|BranchCodes|LinkedStudentIds|DeletedAt|InstitutionId|BusinessId)\s*=(?!=)",
        RegexOptions.Compiled);

    private static readonly Regex ClassHeader = new(
        @"^(?:public|internal)\s+(?:static\s+|sealed\s+)*class\s+(\w+)", RegexOptions.Compiled | RegexOptions.Multiline);

    [Fact]
    public void Yetki_alani_yazan_her_sinif_onbellegi_temizler()
    {
        var kok = Path.Combine(RepoRoot(), "src", "Modules", "Security");
        var violations = new List<string>();

        foreach (var file in Directory.EnumerateFiles(kok, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                continue;

            foreach (var (ad, govde) in SplitIntoClasses(File.ReadAllText(file)))
            {
                if (!PermissionFieldWrite.IsMatch(govde)) continue;
                if (govde.Contains("InvalidateCache", StringComparison.Ordinal)) continue;

                violations.Add($"{Path.GetFileName(file)} → {ad}");
            }
        }

        violations.ShouldBeEmpty(
            "Yetki taşıyan alanı yazan sınıf izin önbelleğini temizlemeli (#209), yoksa değişiklik "
            + "5 dakikaya kadar etkisiz kalır ve yönetici düzeltmenin işe yaramadığını sanar. "
            + "PermissionClaimsTransformation.InvalidateCache(cache, keycloakUserId) çağırın:\n  "
            + string.Join("\n  ", violations.OrderBy(x => x, StringComparer.Ordinal)));
    }

    /// <summary>
    /// Taramanın gerçekten çalıştığını doğrular. Desen bozulup hiçbir şey eşleşmezse test
    /// sessizce yeşil kalırdı — "hiç ihlal yok" ile "hiç bakmadım" ayrı şeylerdir.
    /// </summary>
    [Fact]
    public void Tarama_bilinen_sinif_gorur()
    {
        var kok = Path.Combine(RepoRoot(), "src", "Modules", "Security");
        var covered = new List<string>();

        foreach (var file in Directory.EnumerateFiles(kok, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;

            foreach (var (ad, govde) in SplitIntoClasses(File.ReadAllText(file)))
                if (PermissionFieldWrite.IsMatch(govde))
                    covered.Add(ad);
        }

        covered.ShouldContain("ChangeUserRolesHandler", "Tarama yetki alanı yazımını bulamıyorsa hiçbir şey doğrulamaz.");
        covered.ShouldContain("SyncUsersFromKeycloakHandler", "#209'un konusu olan handler kapsamda olmalı.");
        covered.ShouldContain("DeleteUserHandler", "#210'un mezar taşı yazımı kapsamda olmalı.");
    }

    /// <summary>Kaynağı <c>public/internal class</c> sınırlarından bölerek (ad, gövde) çiftleri üretir.</summary>
    private static IEnumerable<(string Name, string Body)> SplitIntoClasses(string source)
    {
        var matches = ClassHeader.Matches(source);

        for (var i = 0; i < matches.Count; i++)
        {
            var start = matches[i].Index;
            var end = i + 1 < matches.Count ? matches[i + 1].Index : source.Length;
            yield return (matches[i].Groups[1].Value, source[start..end]);
        }
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
