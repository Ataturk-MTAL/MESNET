using MESNET.Common.Infrastructure.Deployment;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// Açılış doğrulayıcısının davranışı — bulgu bulunca ne yaptığı kadar,
/// <b>bulamayınca, ölçemeyince ve açılışı durdurmaya kalkışınca</b> ne yaptığı da kilitlenir.
///
/// <para>Bu kontrolün varlık sebebi sessiz eksikliktir; kendisi sessizce yanlış davranırsa
/// koruduğu şeyin aynısını üretir.</para>
/// </summary>
public class DeploymentPrerequisiteVerificationTests
{
    [Fact]
    public async Task Sonda_yoksa_acilis_durmaz_ve_temiz_denmez()
    {
        var (service, log) = Build();

        await Should.NotThrowAsync(() => service.StartAsync(CancellationToken.None));

        // Hiç ölçüm yapılmadıysa sonuç "temiz" DEĞİLDİR; sessiz kalmak tam da kapatılmak
        // istenen hatadır.
        log.ShouldContain(e => e.Level == LogLevel.Warning && e.Message.Contains("kayıtlı sonda yok"));
    }

    [Fact]
    public async Task Bulgu_yoksa_kritik_log_yazilmaz()
    {
        var (service, log) = Build(new FakeProbe("Temiz", finding: null));

        await service.StartAsync(CancellationToken.None);

        log.ShouldNotContain(e => e.Level == LogLevel.Critical);
    }

    [Fact]
    public async Task Bulgu_varken_bile_acilis_DURMAZ()
    {
        var (service, _) = Build(new FakeProbe(
            "Yönetici bağı görünümü",
            new PrerequisiteFinding("12 okul kayıtlı; görünümde 0 satır var.", "Pano her okulu yöneticisiz sayar.")));

        // KİLİTLENME KİLİDİ. Kardeş doğrulayıcılar (Realm, DocumentTenancy) Development'ta atar;
        // bu atmaz çünkü çaresi BU API'nin kendi ucudur. Atsaydı boş bir veritabanında açılış
        // hiç tamamlanmaz, uç ulaşılamaz olur ve sistem kendi çaresine erişemezdi.
        // Bu testi "Development'ta atsın" diye değiştirmek, o kilitlenmeyi geri getirir.
        await Should.NotThrowAsync(() => service.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Bulgu_kritik_seviyede_olcum_sonuc_ve_adim_ile_yazilir()
    {
        var (service, log) = Build(new FakeProbe(
            "Yönetici bağı görünümü",
            new PrerequisiteFinding("12 okul kayıtlı; görünümde 0 satır var.", "Pano her okulu yöneticisiz sayar."),
            remedy: "POST /api/security/users/replay"));

        await service.StartAsync(CancellationToken.None);

        var kritik = log.Single(e => e.Level == LogLevel.Critical).Message;

        // Operatör bu satırı okuyup koşturur: ölçüm, sonuç ve birebir adım orada olmalı.
        kritik.ShouldContain("12 okul kayıtlı; görünümde 0 satır var.");
        kritik.ShouldContain("Pano her okulu yöneticisiz sayar.");
        kritik.ShouldContain("POST /api/security/users/replay");
    }

    [Fact]
    public async Task Olcum_yapamayan_sonda_bulgu_uretmez()
    {
        var (service, log) = Build(new ThrowingProbe("Tablo henüz yok"));

        // İlk açılışta tablolar henüz yaratılmamış olabilir. Ölçememeyi bulguya çevirmek,
        // doğrulayıcıyı dağıtımı kıran bir engele dönüştürürdü.
        await Should.NotThrowAsync(() => service.StartAsync(CancellationToken.None));
        log.ShouldNotContain(e => e.Level == LogLevel.Critical);
    }

    [Fact]
    public async Task Olculemeyen_sonda_raporda_ADIYLA_gecer()
    {
        var (service, log) = Build(
            new FakeProbe("Kurum ağacı", new PrerequisiteFinding("3 okulun yolu boş.", "Alt ağaç boş görünür.")),
            new ThrowingProbe("Staj saga'sı kopyaları"));

        await service.StartAsync(CancellationToken.None);

        var kritik = log.Single(e => e.Level == LogLevel.Critical).Message;

        // Ölçülemeyeni raporlamayan bir doğrulayıcı, ölçmediği adımı "temiz" gösterir —
        // tam da kapatmaya çalıştığı hata.
        kritik.ShouldContain("ÖLÇÜLEMEDİ");
        kritik.ShouldContain("Staj saga'sı kopyaları");
    }

    private static (DeploymentPrerequisiteVerificationHostedService Service, List<LogEntry> Log) Build(
        params IDeploymentPrerequisiteProbe[] probes)
    {
        var services = new ServiceCollection();
        foreach (var probe in probes)
            services.AddSingleton(probe);

        var provider = services.BuildServiceProvider();
        var log = new List<LogEntry>();

        return (new DeploymentPrerequisiteVerificationHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new CapturingLogger(log)), log);
    }

    private sealed record LogEntry(LogLevel Level, string Message);

    private sealed class CapturingLogger(List<LogEntry> sink)
        : ILogger<DeploymentPrerequisiteVerificationHostedService>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => sink.Add(new LogEntry(logLevel, formatter(state, exception)));
    }

    private sealed class FakeProbe(string name, PrerequisiteFinding? finding, string remedy = "POST /api/x")
        : IDeploymentPrerequisiteProbe
    {
        public string Name => name;
        public string Remedy => remedy;
        public Task<PrerequisiteFinding?> ProbeAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(finding);
    }

    private sealed class ThrowingProbe(string name) : IDeploymentPrerequisiteProbe
    {
        public string Name => name;
        public string Remedy => "POST /api/x";
        public Task<PrerequisiteFinding?> ProbeAsync(CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("relation \"mt_doc_x\" does not exist");
    }
}
