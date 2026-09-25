using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Row-level security açıkken async projeksiyon kullanılmaz (#317).
///
/// <para><b>Ölçülen tehlike:</b> async daemon olayları session DIŞINDA, kiracı ayarı olmayan
/// bir bağlantıda okur. RLS politikası o satırları süzer; daemon boş aralığı "işlendi" sayıp
/// ilerlemesini taşır. Ölçüldü: 6 devamsızlık kaydı yazıldı, <c>AttendanceView:All</c>
/// ilerlemesi 29'a çıktı, görünümde 0 belge, logda HİÇ hata yok. Olaylar kalıcı atlanır;
/// sorun sonradan fark edilse bile atlanan aralık yeniden inşa edilmeden geri gelmez.</para>
///
/// <para>Inline projeksiyon ve snapshot yazan session'da, kiracısı kurulu bağlantıda çalışır.
/// Async gerçekten gerekirse daemon'un kiracı başına çalışması ayrıca tasarlanmalıdır.</para>
/// </summary>
public sealed class AsyncProjectionRlsDriftTests
{
    private static readonly Regex AsyncLifecycle =
        new(@"\b(ProjectionLifecycle|SnapshotLifecycle)\.Async\b", RegexOptions.Compiled);

    [Fact]
    public void Async_projeksiyon_kaydi_yok()
    {
        var violations = Directory
            .EnumerateFiles(Path.Combine(RepoRoot(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => AsyncLifecycle.IsMatch(StripComments(File.ReadAllText(f))))
            .Select(f => Path.GetRelativePath(RepoRoot(), f))
            .ToList();

        violations.ShouldBeEmpty(
            "Async projeksiyon row-level security altında olayları hatasız ATLAR (daemon kiracısız "
            + "okur, ilerlemeyi yine de taşır). Inline kullanın ya da daemon'u kiracı başına "
            + $"çalışacak şekilde tasarlayın (#317). İhlaller: {string.Join(" | ", violations)}");
    }

    private static string StripComments(string source) =>
        Regex.Replace(source, @"//[^\n]*|/\*.*?\*/", string.Empty, RegexOptions.Singleline);

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
