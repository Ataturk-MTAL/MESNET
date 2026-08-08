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
    /// <summary>
    /// <c>options.Schema.For&lt;T&gt;()</c> — Marten'a kayıtlı belge tiplerinin otoritesi.
    ///
    /// <para>Tip adı <b>nitelikli</b> yazılabilir (<c>Schema.For&lt;Core.Entities.Business&gt;()</c>);
    /// desen yalnız <c>\w+</c> arasaydı o çağrılar sessizce taramanın dışında kalırdı — testin
    /// verdiği "her belge sınıflandırıldı" güvencesi yanlış olurdu. İlk sürümde tam olarak bu
    /// oldu: dört tip (<c>Business</c>, <c>Institution</c>, iki saga) kaçmıştı.</para>
    /// </summary>
    private static readonly Regex SchemaForRegex = new(@"Schema\.For<([\w.]+)>", RegexOptions.Compiled);

    private static HashSet<string> RegisteredDocumentTypes()
    {
        var root = Path.Combine(RepoRoot(), "src");
        var types = new HashSet<string>(StringComparer.Ordinal);

        foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}"))
                continue;

            foreach (Match m in SchemaForRegex.Matches(File.ReadAllText(file)))
            {
                // Nitelikli ad → son segment (Core.Entities.Business → Business).
                var type = m.Groups[1].Value;
                types.Add(type[(type.LastIndexOf('.') + 1)..]);
            }
        }

        return types;
    }

    [Fact]
    public void Kayitli_her_belge_tipi_siniflandirilmis()
    {
        var registered = RegisteredDocumentTypes();
        registered.Count.ShouldBeGreaterThan(30, "Tarama belge tipi bulamadıysa test hiçbir şey doğrulamaz.");

        var unclassified = registered.Where(t => !DocumentTenancyMap.All.ContainsKey(t)).OrderBy(t => t).ToList();

        unclassified.ShouldBeEmpty(
            "Yeni belge tipi kiracılık açısından sınıflandırılmalı (#147): kiracıya mı ait, "
            + "paylaşımlı mı, kimlik katmanı mı? Karar sonraya bırakılamaz — kiracılık geçişinde "
            + "yanlış damga veri göçü demektir. DocumentTenancyMap'e ekleyin:\n  "
            + string.Join("\n  ", unclassified));
    }

    [Fact]
    public void Siniflandirmada_kayitli_olmayan_tip_yok()
    {
        var registered = RegisteredDocumentTypes();

        var extra = DocumentTenancyMap.All.Keys.Where(t => !registered.Contains(t)).OrderBy(t => t).ToList();

        extra.ShouldBeEmpty(
            "Sınıflandırmada Marten'a kayıtlı olmayan tip var — belge silindiyse girdisi de "
            + "silinmeli, yoksa liste gerçeği yansıtmayı bırakır:\n  " + string.Join("\n  ", extra));
    }

    /// <summary>
    /// <c>MissingKey</c> sınıfı <b>boş kalmalı</b> (#147 adım 1).
    ///
    /// <para>Dört görünüm bu sınıftaydı — <c>StudentNameView</c> (iki modülde),
    /// <c>StudentPaymentProfile</c>, <c>StudentAbsenceView</c>, <c>AttendanceView</c>. Hepsi
    /// öğrenci düzeyinde veri taşıyor ve kiracı anahtarı yoktu; çok-okulda sorgu iki okulun
    /// satırını ayırt edemezdi. Kaynak olaylar (<c>StudentRegistered</c>, <c>AttendanceMarked</c>)
    /// <c>InstitutionId</c>'yi zaten taşıdığı için damgalamak yeterliydi.</para>
    ///
    /// <para><b>Mühür:</b> bu sınıfa yeni belge girmesi artık kabul edilmez. Yeni bir görünüm
    /// kiracıya ait veri taşıyorsa kiracı anahtarını <b>doğduğu anda</b> alır — sonradan eklemek
    /// backfill gerektirir ve o backfill unutulursa sızıntı sessizdir.</para>
    /// </summary>
    [Fact]
    public void Kiracı_anahtari_eksik_belge_kalmadi()
    {
        var missing = DocumentTenancyMap.All
            .Where(kv => kv.Value == DocumentTenancy.MissingKey)
            .Select(kv => kv.Key)
            .OrderBy(t => t)
            .ToList();

        missing.ShouldBeEmpty(
            "Kiracıya ait veri taşıyıp kiracı anahtarı olmayan belge bırakılamaz (#147). "
            + "Kaynak olay InstitutionId taşıyorsa görünümü doğduğu anda damgalayın; sonradan "
            + "eklemek backfill gerektirir ve unutulan backfill sessiz sızıntıdır:\n  "
            + string.Join("\n  ", missing));
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
