using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Kiracı ayarı (<c>app.tenant_id</c>) yalnız tek yerden yazılır (#318, kural 8).
///
/// <para><b>Neden veritabanında engellenemez:</b> özel GUC'leri her rol değiştirebilir. Bir SQL
/// enjeksiyonu <c>set_config('app.tenant_id', 'baska-okul', ...)</c> ile row-level security'nin
/// baktığı kiracıyı değiştirip başka okulun satırlarını okuyabilir. Veritabanı bunu ayırt
/// edemez; yapılabilecek olan, ayarı yazan kodu uygulamada TEK YERE daraltmaktır — öyle ki yeni
/// bir <c>set_config</c> çağrısı gözden geçirmede kaçmasın.</para>
///
/// <para>Marten session bağlantısında ayarı kendisi kurar; uygulama kodu yalnız
/// <c>TenantScopedConnection</c> üzerinden yazar. Ayarın adı da tek sabittedir
/// (<c>TenantRls.SettingName</c>) — Marten'ın politikası ile olay politikası aynı adı okumazsa
/// biri hiçbir satırı göstermez.</para>
/// </summary>
public sealed class TenantSettingWriteDriftTests
{
    private static readonly Regex SetConfig = new(@"\bset_config\s*\(", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex SettingLiteral = new("\"app\\.tenant_id\"", RegexOptions.Compiled);

    private const string Writer = "src/MESNET.Common.Infrastructure/Tenancy/TenantScopedConnection.cs";
    private const string SettingOwner = "src/MESNET.Common.Infrastructure/Tenancy/EventStoreRlsPolicy.cs";

    [Fact]
    public void Kiraci_ayarini_yalniz_TenantScopedConnection_yazar()
    {
        var violations = CodeFiles()
            .Where(f => f.Relative != Writer && SetConfig.IsMatch(f.Code))
            .Select(f => f.Relative)
            .ToList();

        violations.ShouldBeEmpty(
            "set_config yalnız TenantScopedConnection'da çağrılır — kiracı ayarını yazan her yeni yol, "
            + "row-level security'nin baktığı kiracıyı değiştirebilir (#318). "
            + $"İhlaller: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Ayar_adi_tek_sabitte_tanimli()
    {
        var violations = CodeFiles()
            .Where(f => f.Relative != SettingOwner && SettingLiteral.IsMatch(f.Code))
            .Select(f => f.Relative)
            .ToList();

        violations.ShouldBeEmpty(
            "\"app.tenant_id\" dizgesi TenantRls.SettingName dışında yazılmış — ad ayrışırsa bir politika "
            + $"hiçbir satırı göstermez. TenantRls.SettingName kullanın. İhlaller: {string.Join(", ", violations)}");
    }

    /// <summary>Yorumlar atılır: kuralın nedenini anlatan XML doc'lar yasak ifadeyi geçirir.</summary>
    private static IEnumerable<(string Relative, string Code)> CodeFiles()
    {
        var root = RepoRoot();
        return Directory.EnumerateFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Select(f => (
                Path.GetRelativePath(root, f).Replace(Path.DirectorySeparatorChar, '/'),
                Regex.Replace(File.ReadAllText(f), @"//[^\n]*|/\*.*?\*/", string.Empty, RegexOptions.Singleline)));
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
