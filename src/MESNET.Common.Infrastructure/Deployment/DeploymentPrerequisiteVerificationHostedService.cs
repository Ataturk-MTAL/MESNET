using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MESNET.Common.Infrastructure.Deployment;

/// <summary>
/// Açılışta dağıtım ön koşullarının <b>belirtilerini</b> ölçer ve atlanmış adımı görünür kılar.
///
/// <para><b>Neden var:</b> resync/backfill adımları atlanınca sistem hata vermez — özellik
/// sessizce çalışmaz. Belirti hep aynıdır: liste boş gelir, sayı sıfır çıkar, buton hiçbir şey
/// yapmaz. Gerçekten yaşandı: <c>users/replay</c> atlanınca müdürlük panosu <b>her okulu</b>
/// yöneticisiz sayıyordu; hata dönmüyor, log basılmıyordu. Bundan sonra o durum bir kritik log
/// satırıdır.</para>
///
/// <para><b>Ölçer, koşturmaz.</b> Açılıştan resync ucu çağırmak bu depoda üç ayrı nedenle
/// mümkün değildir: (1) Wolverine <c>UseWolverine</c> ile host'tan <b>sonra</b> başlar, açılıştan
/// yapılan her yayın <c>WolverineHasNotStartedException</c> fırlatır; (2) iki uç idempotent
/// değildir ve her yeniden başlatmada sayacı bozar; (3) <c>client_credentials</c> servis hesabının
/// realm rolü yoktur. Koşturma sırası ve kimliği operatörde kalır:
/// <c>src/Docs/docs/infrastructure/dagitim-on-kosullari.md</c>.</para>
///
/// <para><b>HİÇBİR ortamda açılışı durdurmaz</b> — ve bu, kardeş iki doğrulayıcıdan
/// <b>bilinçli bir ayrılıktır</b>. <c>RealmVerificationHostedService</c> ile
/// <c>DocumentTenancyVerificationHostedService</c> Development'ta atar, çünkü onların çaresi
/// süreç dışındadır (Keycloak ayarı, kaynak kodda sınıflandırma). Buradaki çare ise <b>bu API'nin
/// kendi ucudur</b>: <c>POST /api/security/users/replay</c>, <c>POST /api/institutions/rebuild-hierarchy</c>.
/// Açılışı durdursaydık uç ulaşılamaz olurdu ve sistem <b>kendi çaresine erişemeyen</b> bir
/// kilitlenmeye girerdi — üstelik her yeni kurulumda, çünkü boş bir veritabanında bu bulgular
/// tanımı gereği vardır. Koruma değil, tuzak olurdu.</para>
///
/// <para>Görünürlük bu yüzden <b>seviyeden</b> gelir: bulgu <c>LogCritical</c> olarak, ölçüm +
/// sonuç + birebir çağrılabilir adım ile tek blokta yazılır.</para>
///
/// <para><b>Ölçülemeyen sonda bulgu üretmez ve SESSİZ KALMAZ.</b> Sondanın istisnası "atlandı"
/// olarak raporlanır; kapsam her koşuda loglanır. Kaç ön koşulun ölçüldüğünü yazmayan bir
/// doğrulayıcı, ölçmediği adımı "temiz" göstererek tam da kapatmaya çalıştığı hatayı üretirdi.</para>
/// </summary>
public sealed class DeploymentPrerequisiteVerificationHostedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DeploymentPrerequisiteVerificationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Sondalar Scoped'dır (IDocumentStore singleton olsa da modül servisleri scope ister) —
        // hosted service singleton'dır, kendi kapsamını açar.
        using var scope = scopeFactory.CreateScope();
        var probes = scope.ServiceProvider.GetServices<IDeploymentPrerequisiteProbe>().ToList();

        if (probes.Count == 0)
        {
            logger.LogWarning(
                "Dağıtım ön koşul doğrulaması atlandı — kayıtlı sonda yok. Hiçbir ön koşul "
                + "ölçülmemiştir; bu bir 'temiz' sonucu DEĞİLDİR.");
            return;
        }

        var findings = new List<(string Name, string Remedy, PrerequisiteFinding Finding)>();
        var unmeasured = new List<string>();

        foreach (var probe in probes)
        {
            try
            {
                var finding = await probe.ProbeAsync(cancellationToken);
                if (finding is not null)
                    findings.Add((probe.Name, probe.Remedy, finding));
            }
            catch (Exception ex)
            {
                // Ölçememek bulgu değildir. İlk açılışta tablolar henüz yok olabilir; veritabanı
                // geç ayağa kalkmış olabilir. Bunu arızaya çevirmek, doğrulayıcıyı dağıtımın
                // kendisini kıran bir engele dönüştürürdü.
                unmeasured.Add(probe.Name);
                logger.LogWarning(
                    ex,
                    "Ön koşul sondası ölçüm yapamadı: {Probe}. Bu bir sapma bulgusu DEĞİLDİR; "
                    + "o ön koşul doğrulanmamış durumdadır.",
                    probe.Name);
            }
        }

        logger.LogInformation(
            "Dağıtım ön koşulları: {Measured}/{Total} ölçüldü, {Findings} eksik bulundu.",
            probes.Count - unmeasured.Count, probes.Count, findings.Count);

        if (findings.Count == 0)
            return;

        // Atmak yerine yazılır: çare bu API'nin kendi ucudur, açılışı durdurmak onu ulaşılmaz
        // kılardı (bkz. sınıf belgesi).
        logger.LogCritical("{Rapor}", Describe(findings, unmeasured));
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Bulguları operatörün <b>koşturabileceği</b> biçimde yazar: her satırda ölçülen sayı,
    /// atlanırsa görülecek yanlış davranış ve birebir çağrılabilir adım.
    /// </summary>
    private static string Describe(
        IReadOnlyList<(string Name, string Remedy, PrerequisiteFinding Finding)> findings,
        IReadOnlyList<string> unmeasured)
    {
        var lines = new List<string>
        {
            $"Dağıtım ön koşulu karşılanmamış ({findings.Count} adım). Bu adımlar atlanınca sistem "
            + "hata VERMEZ — özellik sessizce çalışmaz.",
            string.Empty,
        };

        foreach (var (name, remedy, finding) in findings)
        {
            lines.Add($"• {name}");
            lines.Add($"    Ölçüm  : {finding.Symptom}");
            lines.Add($"    Sonuç  : {finding.Consequence}");
            lines.Add($"    Adım   : {remedy}");
        }

        lines.Add(string.Empty);
        lines.Add("Sıra ve gerekçe: src/Docs/docs/infrastructure/dagitim-on-kosullari.md");

        if (unmeasured.Count > 0)
        {
            lines.Add(
                $"Ayrıca {unmeasured.Count} ön koşul ÖLÇÜLEMEDİ ve bu listede yoktur: "
                + string.Join(", ", unmeasured));
        }

        return string.Join(Environment.NewLine, lines);
    }
}
