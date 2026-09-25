using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Uçlar isteğin okulunu <c>institution_id</c> claim'inden OKUMAZ; kiracıdan türetir
/// (<c>RequestInstitution.Of</c>).
///
/// <para><b>Neden:</b> claim kullanıcının ev kurumudur, kiracıyı ise aktif bağlam belirler.
/// Bir okula geçen il yetkilisinde ikisi ayrışır: row-level security satırları aktif okula
/// süzer, uç ise ev kurumuna filtreler — sonuç hata değil <b>boş liste</b>. Ölçüldü:
/// Koordinasyon'un 34 ucu bu yüzden bağlam değiştiren kullanıcıya boş dönüyordu.</para>
/// </summary>
public sealed class RequestInstitutionDriftTests
{
    private static readonly Regex RawClaimRead = new(
        "FindFirst(Value)?\\s*\\(\\s*\"institution_id\"|ClaimGuid\\s*\\([^,]+,\\s*\"institution_id\"",
        RegexOptions.Compiled);

    [Fact]
    public void Uclar_kurumu_claimden_degil_kiracidan_turetir()
    {
        var root = RepoRoot();
        var violations = Directory
            .EnumerateFiles(Path.Combine(root, "src", "Modules"), "*.cs", SearchOption.AllDirectories)
            .Where(f => f.Contains(".Api" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => RawClaimRead.IsMatch(
                Regex.Replace(File.ReadAllText(f), @"//[^\n]*|/\*.*?\*/", string.Empty, RegexOptions.Singleline)))
            .Select(f => Path.GetRelativePath(root, f).Replace(Path.DirectorySeparatorChar, '/'))
            .ToList();

        violations.ShouldBeEmpty(
            "Uç institution_id claim'ini okuyor — aktif bağlamdaki kullanıcıda boş sonuç döner. "
            + $"RequestInstitution.Of(http) kullanın. İhlaller: {string.Join(", ", violations)}");
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

        throw new InvalidOperationException("Depo kökü bulunamadı (MESNET.slnx).");
    }
}
