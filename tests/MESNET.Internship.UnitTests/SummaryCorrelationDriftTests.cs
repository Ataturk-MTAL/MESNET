using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Internship.UnitTests;

/// <summary>
/// Staj kaydını <b>öğrenciden</b> bulan her tüketici, aday seçimini
/// <c>SagaCorrelationPolicy</c>'ye devretmek zorundadır (#295).
///
/// <para><b>Neden kilit gerekiyor:</b> <c>Query&lt;InternshipSummary&gt;().FirstOrDefaultAsync(
/// s =&gt; s.StudentId == e.StudentId)</c> derlenir, çalışır, test kırmaz. Kırılan şey görünmez:
/// özet <b>yerleştirme başına</b> doğar, yani fesih + yeniden yerleştirme yaşamış öğrencinin
/// birden çok satırı olur ve sorgu Postgres'in döndürdüğü ilkini alır — o sıra kararlı
/// değildir. Yanlış satır güncellenir, hiçbir yerde hata görünmez.</para>
///
/// <para><b>Neden ortak politika şart:</b> özet ile saga <b>aynı satırı</b> seçmek zorundadır.
/// Ayrı kurallar yazılsaydı özet, saga'nın ilerlettiğinden BAŞKA bir stajı anlatabilir ve iki
/// kayıt sessizce ayrışırdı. Kural ayrıca faz süzgecini de taşır: kapanmış staja olay
/// taşınmaz.</para>
///
/// <para><b>İşaretleme SINIF düzeyindedir</b> — #284'te ölçülen ders: aynı dosyada tek bir
/// sınıfın politikayı çağırması, o dosyadaki diğer sınıfları aklamaz.</para>
/// </summary>
public class SummaryCorrelationDriftTests
{
    /// <summary>Staj kaydını koleksiyon olarak tarayan sorgu.</summary>
    private static readonly Regex StudentScopedQuery = new(
        @"Query<(?:InternshipSummary|InternshipSaga)>\s*\(\s*\)", RegexOptions.Compiled);

    /// <summary>Aday seçiminin politikaya devredildiğini gösteren ÇAĞRI.</summary>
    private static readonly Regex PolicyCall = new(
        @"SagaCorrelationPolicy\s*\.\s*(?:MatchesContract|MatchesAttendance)\s*\(", RegexOptions.Compiled);

    /// <summary>
    /// Sınıf ve üye bildirimleri — tarama birimi METOTTUR.
    ///
    /// <para>Sınıf bildirimi de yakalanır ki metot dışı gövde (alan başlatıcıları) kapsam dışı
    /// kalmasın; her blok bir sonraki bildirime kadar sürer.</para>
    /// </summary>
    private static readonly Regex MemberDeclaration = new(
        @"(?m)^\s*(?:public|private|internal|protected)\s+(?:static\s+|sealed\s+|abstract\s+|partial\s+|async\s+|readonly\s+)*(?:class\s+)?(\w[\w<>,\?\[\]\. ]*?)\s*(\w+)\s*[\(<{=]",
        RegexOptions.Compiled);

    /// <summary>
    /// Kaydı <b>kimliğiyle</b> (deterministik) çeken ya da bakım amaçlı tüm satırları dolaşan
    /// sınıflar — aday seçimi yapmadıkları için politikaya ihtiyaçları yoktur.
    ///
    /// <para><c>ResyncInternshipSagasHandler</c>: kopya birleştirme; tanımı gereği TÜM saga'ları
    /// tarar ve seçimi <c>PlacementId</c> gruplaması + faz sırasıyla yapar, öğrenciden aday
    /// aramaz. <c>InternshipSagaDuplicateProbe</c> ve <c>GetStuckApprovalsHandler</c>: salt
    /// okuma/ölçüm, tek kayıt seçmezler. <c>ListInternshipsHandler</c>: listeleme ucu — ÇOK
    /// satır döndürür, tek aday seçmez ve kendi kapsam merdivenini (<c>OwnDataScope</c>, #182)
    /// uygular; politika buraya uygulansaydı liste tek satıra inerdi.</para>
    /// </summary>
    private static readonly HashSet<string> Exempt = new(StringComparer.Ordinal)
    {
        "ResyncInternshipSagasHandler",
        "InternshipSagaDuplicateProbe",
        "GetStuckApprovalsHandler",
        "ListInternshipsHandler",
    };

    [Fact]
    public void Ogrenciden_aday_arayan_metot_politikayi_kullanir()
    {
        var violations = new List<string>();

        foreach (var file in SourceFiles())
        {
            var code = StripComments(File.ReadAllText(file));
            if (!StudentScopedQuery.IsMatch(code))
                continue;

            foreach (var (member, body) in MemberBlocks(code))
            {
                if (!StudentScopedQuery.IsMatch(body)) continue;
                if (PolicyCall.IsMatch(body)) continue;
                if (Exempt.Contains(member)) continue;
                if (Exempt.Contains(Path.GetFileNameWithoutExtension(file))) continue;

                violations.Add($"{Path.GetFileName(file)}::{member}");
            }
        }

        violations.ShouldBeEmpty(
            "Staj kaydını öğrenciden arayan bir METOT SagaCorrelationPolicy'yi çağırmıyor. "
            + "Öğrencinin birden çok stajı olabilir (fesih + yeniden yerleştirme) ve sıralamasız "
            + "FirstOrDefault, Postgres'in döndürdüğü satırı alır — kararlı değildir, yanlış kayıt "
            + "güncellenir ve hiçbir yerde hata görünmez. Ayrıca özet ile saga AYNI satırı seçmek "
            + $"zorundadır. Gerçekten aday seçmiyorsa muafiyet listesine gerekçesiyle yazın. İhlaller: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Kilit_gercek_dosya_tariyor()
    {
        // Tarama boş küme dönerse yukarıdaki test hiçbir şey kanıtlamadan yeşil kalırdı.
        SourceFiles()
            .Where(f => StudentScopedQuery.IsMatch(StripComments(File.ReadAllText(f))))
            .ShouldNotBeEmpty("Hiçbir dosyada staj kaydı sorgusu bulunamadı — desen değişmiş olabilir.");
    }

    private static IEnumerable<(string Member, string Body)> MemberBlocks(string code)
    {
        var declarations = MemberDeclaration.Matches(code);

        if (declarations.Count == 0)
        {
            yield return ("(üyesiz)", code);
            yield break;
        }

        for (var i = 0; i < declarations.Count; i++)
        {
            var start = declarations[i].Index;
            var end = i + 1 < declarations.Count ? declarations[i + 1].Index : code.Length;
            yield return (declarations[i].Groups[2].Value, code[start..end]);
        }
    }

    private static List<string> SourceFiles() =>
        Directory.EnumerateFiles(
                Path.Combine(RepoRoot(), "src", "Modules", "Internship"), "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                     && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .ToList();

    private static string StripComments(string source)
    {
        var withoutBlocks = Regex.Replace(source, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        return Regex.Replace(withoutBlocks, @"//.*$", string.Empty, RegexOptions.Multiline);
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

        throw new InvalidOperationException("Depo kökü bulunamadı.");
    }
}
