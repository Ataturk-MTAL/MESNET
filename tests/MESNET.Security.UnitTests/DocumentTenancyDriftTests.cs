using System.Text.RegularExpressions;
using MESNET.Common.Shared.Tenancy;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Her belge tipi kiracılık açısından <b>bilinçli olarak sınıflandırılmış</b> olmalı (#147).
///
/// <para><b>Neden kilit gerekiyor:</b> kiracılık geçişinin (#149) sert ön koşulu "hangi belge
/// kiracıya ait, hangisi paylaşımlı" listesidir ve <c>AllDocumentsAreMultiTenanted()</c> toptan
/// kullanılamaz — kullanılsaydı ulusal alan/dal kataloğu, ulusal ücret parametreleri, kimlik
/// katmanı ve <b>paylaşımlı işletme kataloğu</b> da kiracı damgası alırdı. Damga bir kez
/// atıldıktan sonra geri almak veri göçü demektir.</para>
///
/// <para>Liste dokümanda tutulsaydı kodla birlikte kayardı. Burada durunca yeni bir belge
/// eklemek kiracı/paylaşımlı kararını <b>zorunlu</b> kılar: karar unutulamaz, test kırılır.</para>
/// </summary>
public sealed class DocumentTenancyDriftTests
{
    /// <summary><c>options.Schema.For&lt;T&gt;()</c> — Marten'a kayıtlı belge tiplerinin otoritesi.</summary>
    private static readonly Regex SchemaForRegex = new(@"Schema\.For<(\w+)>", RegexOptions.Compiled);

    private static HashSet<string> KayitliBelgeTipleri()
    {
        var root = Path.Combine(RepoRoot(), "src");
        var tipler = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                continue;

            foreach (Match m in SchemaForRegex.Matches(File.ReadAllText(file)))
                tipler.Add(m.Groups[1].Value);
        }

        return tipler;
    }

    [Fact]
    public void Kayitli_her_belge_tipi_siniflandirilmis()
    {
        var kayitli = KayitliBelgeTipleri();
        kayitli.Count.ShouldBeGreaterThan(30, "Tarama belge tipi bulamadıysa test hiçbir şey doğrulamaz.");

        var siniflandirilmamis = kayitli.Where(t => !DocumentTenancyMap.All.ContainsKey(t)).OrderBy(t => t).ToList();

        siniflandirilmamis.ShouldBeEmpty(
            "Yeni belge tipi kiracılık açısından sınıflandırılmalı (#147): kiracıya mı ait, "
            + "paylaşımlı mı, kimlik katmanı mı? Karar sonraya bırakılamaz — kiracılık geçişinde "
            + "yanlış damga veri göçü demektir. DocumentTenancyMap'e ekleyin:\n  "
            + string.Join("\n  ", siniflandirilmamis));
    }

    [Fact]
    public void Siniflandirmada_kayitli_olmayan_tip_yok()
    {
        var kayitli = KayitliBelgeTipleri();

        var fazla = DocumentTenancyMap.All.Keys.Where(t => !kayitli.Contains(t)).OrderBy(t => t).ToList();

        fazla.ShouldBeEmpty(
            "Sınıflandırmada Marten'a kayıtlı olmayan tip var — belge silindiyse girdisi de "
            + "silinmeli, yoksa liste gerçeği yansıtmayı bırakır:\n  " + string.Join("\n  ", fazla));
    }

    /// <summary>
    /// <c>MissingKey</c> geçici bir sınıftır: kiracıya ait veri taşıyıp kiracı anahtarı olmayan
    /// belgeler. Bu testin amacı listeyi <b>görünür</b> tutmak — sayı arttığında fark edilsin.
    /// Kiracılık geçişinden önce bu sınıfın boşalması gerekir.
    /// </summary>
    [Fact]
    public void Kiracı_anahtari_eksik_belgeler_bilinen_listede()
    {
        var eksik = DocumentTenancyMap.All
            .Where(kv => kv.Value == DocumentTenancy.MissingKey)
            .Select(kv => kv.Key)
            .OrderBy(t => t)
            .ToList();

        eksik.ShouldBe(
            ["AttendanceView", "StudentAbsenceView", "StudentNameView", "StudentPaymentProfile"],
            ignoreOrder: true,
            customMessage:
            "Kiracı anahtarı eksik belge listesi değişti. Yeni bir belge bu sınıfa girdiyse "
            + "gerekçesi yazılmalı; çıktıysa bu test güncellenmeli. Liste kiracılık geçişinden "
            + "önce BOŞALMALIDIR (#149) — o belgeler çok-okulda iki okulun satırını ayırt edemez.");
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
