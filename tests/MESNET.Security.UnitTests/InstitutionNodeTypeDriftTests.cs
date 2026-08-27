using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// <b><c>Institution</c> artık "okul" demek değil.</b>
///
/// <para>Kurum belgesi ağacın düğümüdür: il müdürlüğü, ilçe müdürlüğü ve okul aynı tiptir.
/// Okul listesi üreten ve düğüm tipine göre süzmeyen bir sorgu, il/ilçe müdürlüğünü okul
/// sanar — ve bu <b>sessizce</b> olur: bir açılır listede "Ankara İl Millî Eğitim Müdürlüğü"
/// belirir, istek 200 döner, log temiz kalır. Ne derleyici ne de mevcut testler görür.</para>
///
/// <para><b>Neden kaynak taraması, neden çalışma zamanı testi değil:</b> tehlike bir davranış
/// hatası değil, <b>unutulmuş bir süzgeç</b>. Unutulan süzgeci ancak "her sorgu şu
/// fonksiyondan geçmeli" kuralını tarayarak yakalayabilirsiniz; davranış testi yalnız
/// yazdığınız senaryoları görür ve unutulan yeni sorgu tanımı gereği yazılmamış olandır.
/// Aynı gerekçe <c>InstitutionScopeDriftTests</c>'te de var.</para>
/// </summary>
public sealed class InstitutionNodeTypeDriftTests
{
    private const string InstitutionApplicationPath = "Modules/Institution/MESNET.Institution.Application";

    /// <summary>Kurum belgesini koleksiyon olarak sorgulayan çağrılar.</summary>
    private static readonly Regex QueriesInstitutions = new(
        @"Query<(Core\.Entities\.)?Institution(Record)?>\(\)", RegexOptions.Compiled);

    /// <summary>Düğüm tipi süzgeci — tek ve taranabilir hedef.</summary>
    private static readonly Regex FiltersNodeType = new(@"\.OfNodeType\(", RegexOptions.Compiled);

    /// <summary>
    /// Düğüm tipine göre süzmesi <b>beklenmeyen</b> yerler.
    ///
    /// <para><c>RebuildInstitutionHierarchyHandler</c>: ağacı kurmak tanımı gereği bütün
    /// düğümleri görmeyi gerektirir. Uç kurum üstü izinle korunur
    /// (<c>platform:tenant:manage</c>) ve komut hiçbir kurum kimliği taşımaz.</para>
    /// </summary>
    private static readonly HashSet<string> MayIgnoreNodeType = new(StringComparer.Ordinal)
    {
        "RebuildInstitutionHierarchyHandler.cs",
    };

    [Fact]
    public void Kurum_sorgusu_dugum_tipine_gore_suzer()
    {
        var offenders = new List<string>();

        foreach (var file in SourceFilesUnder(Path.Combine(RepoRoot(), "src", InstitutionApplicationPath)))
        {
            var name = Path.GetFileName(file);
            if (MayIgnoreNodeType.Contains(name)) continue;

            var text = File.ReadAllText(file);
            if (!QueriesInstitutions.IsMatch(text)) continue;
            if (FiltersNodeType.IsMatch(text)) continue;

            offenders.Add(name);
        }

        offenders.ShouldBeEmpty(
            "Kurum belgesi düğüm tipi süzülmeden sorgulanıyor. Institution artık 'okul' demek "
            + "değil: il ve ilçe müdürlüğü düğümleri de aynı belgedir. Süzmeyen sorgu onları "
            + "OKUL SANAR ve bu sessizce olur — açılır listede bir MEB müdürlüğü adı belirir, "
            + "istek 200 döner, log temiz kalır. Sorguya .OfNodeType(...) ekleyin; gerçekten "
            + "bütün düğümleri görmesi gereken bir işse muafiyet listesine gerekçesiyle yazın. "
            + $"İhlaller: {string.Join(", ", offenders)}");
    }

    /// <summary>
    /// Muafiyet listesi <b>küçük kalmalı</b>. Büyümesi, kuralın kural olmaktan çıkıp
    /// istisnalar tablosuna dönüştüğünün işaretidir (<c>InstitutionScopeDriftTests</c> ile
    /// aynı gerekçe).
    /// </summary>
    [Fact]
    public void Muafiyet_listesi_kucuk_kalir()
    {
        MayIgnoreNodeType.Count.ShouldBeLessThanOrEqualTo(2);
    }

    /// <summary>
    /// <b>Liste bayatlamaz.</b> Muafiyet verilen dosya silinirse satır da silinmelidir;
    /// yoksa liste zamanla gerçekle ilgisini kaybeder.
    /// </summary>
    [Fact]
    public void Muafiyet_listesinde_olu_satir_kalmaz()
    {
        var existing = SourceFilesUnder(Path.Combine(RepoRoot(), "src", InstitutionApplicationPath))
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.Ordinal);

        var stale = MayIgnoreNodeType.Where(f => !existing.Contains(f)).ToList();

        stale.ShouldBeEmpty(
            $"Muafiyet listesinde artık var olmayan dosya var: {string.Join(", ", stale)}");
    }

    private static IEnumerable<string> SourceFilesUnder(string root)
    {
        var obj = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
        var bin = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

        return Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains(obj, StringComparison.Ordinal)
                     && !f.Contains(bin, StringComparison.Ordinal));
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
