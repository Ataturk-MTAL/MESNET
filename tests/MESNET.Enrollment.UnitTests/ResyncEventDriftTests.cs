using System.Text.RegularExpressions;
using Shouldly;
using Xunit;

namespace MESNET.Enrollment.UnitTests;

/// <summary>
/// Onarım (resync) yolları <b>yaşam döngüsü başlatıcı</b> olay yayınlayamaz (#291).
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
/// </summary>
public class ResyncEventDriftTests
{
    /// <summary>
    /// Saga'yı başlatan olaylar. Yeni bir <c>Start(...)</c> girişi eklenirse bu liste de
    /// büyümelidir — aksi hâlde kilit yeni başlatıcıyı görmez.
    /// </summary>
    private static readonly string[] SagaStartingEvents = ["StudentPlaced"];

    [Fact]
    public void Resync_handlerlari_yasam_dongusu_baslatici_olay_yayinlamaz()
    {
        var violations = new List<string>();

        foreach (var file in ResyncHandlerFiles())
        {
            var code = File.ReadAllText(file);

            foreach (var starter in SagaStartingEvents)
            {
                var publish = new Regex($@"PublishAsync\s*\(\s*new\s+{starter}\s*\(");
                if (publish.IsMatch(code))
                    violations.Add($"{Path.GetFileName(file)} → {starter}");
            }
        }

        violations.ShouldBeEmpty(
            "Onarım handler'ı saga başlatıcı olayı yeniden yayınlıyor. Uç 200 döner ama saga "
            + "INSERT'i tekil kısıt ihlaliyle ölü mektuba düşer ve kapasite bozulur — hiçbir "
            + "yerde hata görünmez. Onarım için ayrı bir anlık görüntü olayı yayınlayın "
            + $"(PlacementSnapshotResynced, AttendanceSnapshotResynced deseni). İhlaller: {string.Join(", ", violations)}");
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
