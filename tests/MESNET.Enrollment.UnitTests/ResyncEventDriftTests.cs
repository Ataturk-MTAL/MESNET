using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Enrollment.UnitTests;

/// <summary>
/// Onarım (resync) yolları, <b>tekrarı zararlı</b> olan olayları yeniden yayınlayamaz
/// (#291, #290).
///
/// <para><b>Neden kilit gerekiyor:</b> <c>bus.PublishAsync(new StudentPlaced(...))</c> bir
/// resync handler'ında tamamen masum görünür ve derlenir, testler yeşil kalır, uç <b>200
/// döner</b>. Kırılan şey görünmez: <c>InternshipSaga.Start</c> deterministik kimlikli (#251)
/// saga'yı yeniden INSERT etmeye çalışır, tekil kısıt ihlaliyle o kuyruk ölü mektuba düşer ve
/// <c>MultipleHandlerBehavior.Separated</c> yüzünden kardeş kuyruklar commit etmeye devam eder.
/// Sonuç: kapasite bozulur, saga yazılmaz, hiçbir yerde hata görünmez.</para>
///
/// <para>Ölçüldü (#291): <c>POST /api/placements/resync-projections</c> tam olarak bunu
/// yapıyordu.</para>
///
/// <para><b>İkinci zarar türü — SAYAÇ (#290).</b> <c>StudentRegistered</c> tüketicilerinden
/// biri şube sayacını <b>artırıyor</b> ve görünüm öğrenci başına değil <b>şube başına</b> tek
/// satır. Yeniden yayın her koşuda sayacı o şubedeki öğrenci sayısı kadar şişiriyordu; sayı
/// <c>UpsertBranchWorkloadConfigHandler</c> üzerinden öğretmen/grup ihtiyacına giriyor. Yine
/// sessiz: uç 200 döner, log temiz kalır, tek iz yanlış bir sayıdır.</para>
/// </summary>
public class ResyncEventDriftTests
{
    /// <summary>
    /// Onarım yolundan <b>yeniden yayınlanamayacak</b> olaylar ve nedenleri.
    ///
    /// <para>Liste elle tutulur çünkü "zararlı tekrar"ın iki ayrı biçimi var ve ikisi de metin
    /// taramasıyla türetilemez: yaşam döngüsü başlatmak (saga <c>Start</c>) ve sayaç artırmak.
    /// Yeni bir saga başlatıcısı ya da yeni bir artırımlı tüketici eklendiğinde bu liste de
    /// büyümelidir — aksi hâlde kilit onu görmez.</para>
    /// </summary>
    private static readonly (string Event, string Reason)[] NonRepublishableEvents =
    [
        ("StudentPlaced", "InternshipSaga'nın başlatıcı olayı (#291) — ikinci yayın tekil kısıt ihlaliyle ölü mektuba düşer"),
        ("StudentRegistered", "Coordination.StudentRegisteredCountConsumer şube sayacını ARTIRIR (#290) — görünüm şube başına tek satır"),
    ];

    [Fact]
    public void Resync_handlerlari_tekrari_zararli_olay_yayinlamaz()
    {
        var violations = new List<string>();

        foreach (var file in ResyncHandlerFiles())
        {
            var code = File.ReadAllText(file);

            foreach (var (name, reason) in NonRepublishableEvents)
            {
                var publish = new Regex($@"PublishAsync\s*\(\s*new\s+{name}\s*\(");
                if (publish.IsMatch(code))
                    violations.Add($"{Path.GetFileName(file)} → {name} ({reason})");
            }
        }

        violations.ShouldBeEmpty(
            "Onarım handler'ı, tekrarı zararlı bir olayı yeniden yayınlıyor. Her iki zarar türü "
            + "de SESSİZDİR: uç 200 döner, log temiz kalır. Onarım için ayrı bir anlık görüntü "
            + "olayı yayınlayın (PlacementSnapshotResynced / StudentSnapshotResynced / "
            + $"AttendanceSnapshotResynced deseni). İhlaller: {string.Join(", ", violations)}");
    }

    [Fact]
    public void Onarim_olayini_sayac_tuketicisi_TUKETMEZ()
    {
        var counters = new[]
        {
            "src/Modules/Coordination/MESNET.Coordination.Application/Consumers/StudentRegisteredCountConsumer.cs",
            "src/Modules/Coordination/MESNET.Coordination.Application/Consumers/StudentDeregisteredCountConsumer.cs",
        };

        foreach (var relative in counters)
        {
            var file = Path.Combine(RepoRoot(), relative);
            File.Exists(file).ShouldBeTrue($"Sayaç tüketicisi taşınmış: {relative}. Kilit artık hiçbir şey korumuyor.");

            // Bu dosyaya StudentSnapshotResynced aşırı yüklemesi eklemek, #290'ı geri alır:
            // onarım yolu sayacı yeniden artırmaya başlar. Sayacın onarımı MUTLAK yoldan
            // (SyncStudentCounts) gelir, artırımla değil.
            File.ReadAllText(file).ShouldNotContain("StudentSnapshotResynced");
        }
    }

    [Fact]
    public void Onarim_olayini_saga_TUKETMEZ()
    {
        var saga = Path.Combine(
            RepoRoot(), "src/Modules/Internship/MESNET.Internship.Application/Sagas/InternshipSaga.cs");

        File.Exists(saga).ShouldBeTrue($"Saga dosyası taşınmış: {saga}. Kilit artık hiçbir şey korumuyor.");

        // Saga bu olayı tüketirse onarım yolu yine saga'ya dokunur ve düzeltme geri alınmış olur.
        File.ReadAllText(saga).ShouldNotContain("PlacementSnapshotResynced");
    }

    [Fact]
    public void Kilit_gercek_dosya_tariyor()
    {
        // Tarama boş küme dönerse yukarıdaki iki test hiçbir şey kanıtlamadan yeşil kalırdı.
        ResyncHandlerFiles().ShouldNotBeEmpty("Hiç resync handler'ı bulunamadı — desen değişmiş olabilir.");
    }

    private static List<string> ResyncHandlerFiles() =>
        Directory.EnumerateFiles(Path.Combine(RepoRoot(), "src", "Modules"), "*.cs", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f).Contains("Resync", StringComparison.Ordinal)
                     && Path.GetFileName(f).EndsWith("Handler.cs", StringComparison.Ordinal))
            .ToList();

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MESNET.slnx")))
            dir = dir.Parent;

        return dir?.FullName ?? throw new InvalidOperationException("Depo kökü bulunamadı.");
    }
}
