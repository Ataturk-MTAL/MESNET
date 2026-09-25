using System.Text.RegularExpressions;
using MESNET.Common.Shared.Tenancy;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Ham bağlantıyla kiracıya ait tablo okunmaz (#317).
///
/// <para><b>Ölçülen tehlike:</b> row-level security kiracıyı <c>app.tenant_id</c> ayarından
/// okur ve Marten o ayarı YALNIZ session bağlantısında kurar. <c>CreateConnection()</c> ile
/// açılan bağlantıda ayar ya hiç yoktur ya da önceki session'ın bıraktığı boş dizedir — tek
/// bağlantılı havuzda ölçüldü: kiracılı tablo <b>hatasız 0 satır</b> döndü. İşletme kümeleme
/// haritası böyle sessizce boşalırdı.</para>
///
/// <para><b>Neden kaynak taraması:</b> derleyici SQL dizgesinin hangi tabloyu okuduğunu
/// bilmez; davranış testi de ancak o uç için yazılmışsa yakalar. Kural: ham SQL'de adı geçen
/// her <c>mt_doc_*</c> tablosu <see cref="DocumentTenancyMap"/>'te aranır; <c>Tenant</c>
/// sınıflıysa dosya <c>TenantScopedConnection</c> kullanmak zorundadır.</para>
/// </summary>
public sealed class RawConnectionTenancyDriftTests
{
    private static readonly Regex RawConnection = new(@"\bCreateConnection\s*\(\s*\)", RegexOptions.Compiled);
    private static readonly Regex DocTable = new(@"\bmt_doc_([a-z0-9_]+)\b", RegexOptions.Compiled);

    /// <summary>Kiracıyı kuran yardımcının kendisi — ham bağlantıyı o açar.</summary>
    private const string Helper = "src/MESNET.Common.Infrastructure/Tenancy/TenantScopedConnection.cs";

    [Fact]
    public void Ham_baglanti_kiraciya_ait_tablo_okumaz()
    {
        var tenantTables = DocumentTenancyMap.All
            .Where(kv => kv.Value == DocumentTenancy.Tenant)
            .Select(kv => kv.Key.ToLowerInvariant())
            .ToHashSet(StringComparer.Ordinal);

        var violations = new List<string>();
        foreach (var file in SourceFiles())
        {
            var relative = Relative(file);
            if (relative == Helper)
                continue;

            var code = File.ReadAllText(file);
            if (!RawConnection.IsMatch(code))
                continue;

            var touched = DocTable.Matches(code)
                .Select(m => m.Groups[1].Value)
                .Where(tenantTables.Contains)
                .Distinct()
                .ToList();

            if (touched.Count > 0)
                violations.Add($"{relative} → {string.Join(", ", touched)}");
        }

        violations.ShouldBeEmpty(
            "Ham bağlantı (CreateConnection) kiracıya ait tablo okuyor. Row-level security açıkken "
            + "bu bağlantıda kiracı ayarı yoktur ve sorgu hatasız BOŞ döner. "
            + "TenantScopedConnection.OpenAsync kullanın (#317). İhlaller: "
            + string.Join(" | ", violations));
    }

    /// <summary>Kilit gerçekten ısırıyor mu — sınıflandırma haritası boş kalmamalı.</summary>
    [Fact]
    public void Haritada_kiraciya_ait_belge_var()
    {
        DocumentTenancyMap.All.Count(kv => kv.Value == DocumentTenancy.Tenant)
            .ShouldBeGreaterThan(0, "Harita boş — kilit hiçbir şeyi denetlemezdi.");
    }

    private static IEnumerable<string> SourceFiles() =>
        Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                        && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

    private static string Relative(string file) =>
        Path.GetRelativePath(RepoRoot(), file).Replace(Path.DirectorySeparatorChar, '/');

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
