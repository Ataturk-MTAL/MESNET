using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Hiçbir tüketici <b>kiracı daraltması olmadan</b> geniş bildirim hedefi kurmasın (#266).
///
/// <para><b>Ölçülmüş sızıntı:</b> <c>NotificationTarget { RequiredPermission = ... }</c> kurum
/// süzgeci olmadan yayınlanıyordu ve bir okulun dekont gecikmesi, <b>öğrenci adı payload'da</b>
/// olacak şekilde, o izne sahip <b>tüm okulların</b> kullanıcılarına ulaşıyordu. Aynı sınıf hata
/// belge bildiriminde de vardı.</para>
///
/// <para><b>Politika artık böyle bir hedefi reddediyor</b> (kimseye ulaşmaz) ve servis uyarı
/// yazıyor. Bu test bir adım öteye geçer: hatayı <b>derleme zamanında</b> yakalar, çünkü
/// çalışma zamanı uyarısı okunmayabilir.</para>
///
/// <para><b>Sınır:</b> tarama tek bir nesne başlatıcısını görür. Hedef bir değişkende parça
/// parça kurulursa ya da <c>with</c> ile türetilirse kaçar. Yine de bugünkü yazım biçimini
/// (tek başlatıcı) korumaya yeter.</para>
/// </summary>
public sealed class NotificationTargetScopeDriftTests
{
    /// <summary>Tek satırlık ya da çok satırlı <c>new NotificationTarget { ... }</c> blokları.</summary>
    private static readonly Regex TargetInitializer = new(
        @"new\s+NotificationTarget\s*\{(?<body>[^}]*)\}",
        RegexOptions.Compiled | RegexOptions.Singleline);

    [Fact]
    public void Genis_hedefler_kurum_daraltmasi_tasir()
    {
        var violations = new List<string>();

        foreach (var file in SourceFiles())
        {
            var code = StripComments(File.ReadAllText(file));

            foreach (Match match in TargetInitializer.Matches(code))
            {
                var body = match.Groups["body"].Value;

                var hasBroad = body.Contains("Roles", StringComparison.Ordinal)
                               || body.Contains("RequiredPermission", StringComparison.Ordinal);

                if (!hasBroad) continue;
                if (body.Contains("InstitutionId", StringComparison.Ordinal)) continue;

                violations.Add($"{Relative(file)}: {Collapse(match.Value)}");
            }
        }

        violations.ShouldBeEmpty(
            "Rol/izin hedefi kiracı sınırını KORUMAZ; InstitutionId ile daraltılmalıdır. "
            + "Daraltmasız hedef bildirimi tüm okullara sızdırır — ölçüldü: bir okulun dekont "
            + "gecikmesi öğrenci adıyla birlikte tüm okulların onaycılarına gidiyordu. "
            + $"İhlaller: {string.Join(" | ", violations)}");
    }

    private static string Collapse(string value) =>
        Regex.Replace(value, @"\s+", " ").Trim();

    private static string StripComments(string source)
    {
        var withoutBlocks = Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        var withoutXmlDoc = Regex.Replace(withoutBlocks, @"^\s*///.*$", string.Empty, RegexOptions.Multiline);
        return Regex.Replace(withoutXmlDoc, @"//.*$", string.Empty, RegexOptions.Multiline);
    }

    private static IEnumerable<string> SourceFiles()
    {
        var root = Path.Combine(RepoRoot(), "src");
        var obj = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        var bin = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

        return Directory
            .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains(obj, StringComparison.Ordinal))
            .Where(p => !p.Contains(bin, StringComparison.Ordinal));
    }

    private static string Relative(string file) =>
        Path.GetRelativePath(RepoRoot(), file);

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MESNET.slnx")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Depo kökü bulunamadı (MESNET.slnx).");
    }
}
