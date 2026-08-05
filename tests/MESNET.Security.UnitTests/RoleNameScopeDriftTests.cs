using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Kapsam kararı rol adına bakamaz — ADR-0001'in kod tarafı kilidi (#184).
///
/// <para><b>Neden dosya taraması:</b> bu kural tek bir sınıfın davranışı değil, kod tabanı geneli
/// bir yasaktır. Davranış testi yalnız bugün var olan çağrı yerlerini korur; yarın başka bir
/// handler'a eklenen <c>IsInRole</c>'ü hiçbir birim testi görmez. Borç tam da böyle birikmişti:
/// #129'da bir yere elle rol adı eklendi, #172'de başka bir rol eklenmesi <b>unutuldu</b> ve
/// işletme İK sessizce yanlış kapsama düştü.</para>
///
/// <para>Yasak <c>ICurrentUserService.IsInRole</c> içindir. Rol <b>atama/yönetim</b> kodu
/// (Keycloak realm rolleri, <c>MesnetRoles</c> kataloğu) kapsam kararı vermez, dokunulmaz.</para>
/// </summary>
public sealed class RoleNameScopeDriftTests
{
    [Fact]
    public void Modul_kodunda_IsInRole_cagrisi_yok()
    {
        var modulesRoot = Path.Combine(RepoRoot(), "src", "Modules");
        Directory.Exists(modulesRoot).ShouldBeTrue($"Modül klasörü bulunamadı: {modulesRoot}");

        var ihlaller = new List<string>();

        foreach (var file in Directory.EnumerateFiles(modulesRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                continue;

            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                // Yorum satırları serbest — kararın NEDEN rol adına bakmadığını anlatan
                // açıklamalar bu dosyaların çoğunda var.
                if (line.TrimStart().StartsWith("//", StringComparison.Ordinal)
                    || line.TrimStart().StartsWith("///", StringComparison.Ordinal))
                    continue;

                if (line.Contains("IsInRole", StringComparison.Ordinal))
                    ihlaller.Add($"{Path.GetRelativePath(RepoRoot(), file)}:{i + 1}");
            }
        }

        ihlaller.ShouldBeEmpty(
            "Kapsam kararı rol adına bakamaz (ADR-0001). Rol adı organizasyon şemasının bugünkü "
            + "fotoğrafıdır ve o şema kayar: yeni bir rol eklendiğinde bu kontrol sessizce yanlış "
            + "çalışır, hata vermez. Kararı izne, claim'e ya da kayda dayandırın — desen: "
            + "PlacementScopePolicy (kapsam merdiveni).\n  " + string.Join("\n  ", ihlaller));
    }

    /// <summary>
    /// Uçlar <c>RequireRole</c> ile korunamaz — yalnız <c>RequireAuthorization(Permissions.X)</c>.
    /// </summary>
    [Fact]
    public void Uclarda_RequireRole_yok()
    {
        var srcRoot = Path.Combine(RepoRoot(), "src");
        var ihlaller = Directory
            .EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
            .Where(f => File.ReadAllText(f).Contains("RequireRole", StringComparison.Ordinal))
            .Select(f => Path.GetRelativePath(RepoRoot(), f))
            .ToList();

        ihlaller.ShouldBeEmpty(
            "Uçlar permission ile korunur, rol ile değil (ADR-0001):\n  "
            + string.Join("\n  ", ihlaller));
    }

    /// <summary>
    /// Test derlemesi depo içinde değil <c>bin/</c> altında koşar; göreli yol doğrudan
    /// kullanılamaz — çözüm dosyası (<c>MESNET.slnx</c>) işaretçi olarak aranır.
    /// </summary>
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
