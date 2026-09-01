using MESNET.Common.Infrastructure.Deployment;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// Açılış doğrulayıcısının davranışı — bulgu bulunca ne yaptığı kadar,
/// <b>bulamayınca ve ölçemeyince</b> ne yaptığı da kilitlenir.
///
/// <para>Bu kontrolün varlık sebebi sessiz eksikliktir; kendisi sessizce yanlış davranırsa
/// koruduğu şeyin aynısını üretir.</para>
/// </summary>
public class DeploymentPrerequisiteVerificationTests
{
    [Fact]
    public async Task Sonda_yoksa_acilis_durmaz()
    {
        // Arrange
        var service = Build(development: true);

        // Act + Assert — kayıtlı sonda yokken açılışı durdurmak, hiçbir ölçüm yapmadan
        // sistemi arızalı ilan etmek olurdu.
        await Should.NotThrowAsync(() => service.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Bulgu_yoksa_acilis_durmaz()
    {
        var service = Build(development: true, new FakeProbe("Temiz", finding: null));

        await Should.NotThrowAsync(() => service.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Development_ortaminda_bulgu_acilisi_durdurur_ve_adimi_yazar()
    {
        var probe = new FakeProbe(
            "Yönetici bağı görünümü",
            new PrerequisiteFinding("12 okul kayıtlı; görünümde 0 satır var.", "Pano her okulu yöneticisiz sayar."),
            remedy: "POST /api/security/users/replay");

        var service = Build(development: true, probe);

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => service.StartAsync(CancellationToken.None));

        // Operatör hata metnini okuyup koşturur: ölçüm, sonuç ve birebir adım orada olmalı.
        ex.Message.ShouldContain("12 okul kayıtlı; görünümde 0 satır var.");
        ex.Message.ShouldContain("Pano her okulu yöneticisiz sayar.");
        ex.Message.ShouldContain("POST /api/security/users/replay");
    }

    [Fact]
    public async Task Development_disinda_bulgu_acilisi_durdurmaz()
    {
        var probe = new FakeProbe(
            "Kurum ağacı",
            new PrerequisiteFinding("3 okulun yolu boş.", "Alt ağaç boş görünür."));

        var service = Build(development: false, probe);

        // Çalışan bir sistemi eksik backfill yüzünden indirmek, eksikliğin kendisinden büyük
        // zarar verebilir — diğer ortamlarda LogCritical yazılır, açılış sürer.
        await Should.NotThrowAsync(() => service.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Olcum_yapamayan_sonda_bulgu_uretmez()
    {
        var service = Build(development: true, new ThrowingProbe("Tablo henüz yok"));

        // İlk açılışta tablolar henüz yaratılmamış olabilir. Ölçememeyi arızaya çevirmek,
        // doğrulayıcıyı dağıtımı kıran bir engele dönüştürürdü.
        await Should.NotThrowAsync(() => service.StartAsync(CancellationToken.None));
    }

    [Fact]
    public async Task Olculemeyen_sonda_raporda_ADIYLA_gecer()
    {
        var service = Build(
            development: true,
            new FakeProbe("Kurum ağacı", new PrerequisiteFinding("3 okulun yolu boş.", "Alt ağaç boş görünür.")),
            new ThrowingProbe("Staj saga'sı kopyaları"));

        var ex = await Should.ThrowAsync<InvalidOperationException>(
            () => service.StartAsync(CancellationToken.None));

        // Ölçülemeyeni raporlamayan bir doğrulayıcı, ölçmediği adımı "temiz" gösterir —
        // tam da kapatmaya çalıştığı hata.
        ex.Message.ShouldContain("ÖLÇÜLEMEDİ");
        ex.Message.ShouldContain("Staj saga'sı kopyaları");
    }

    private static DeploymentPrerequisiteVerificationHostedService Build(
        bool development, params IDeploymentPrerequisiteProbe[] probes)
    {
        var services = new ServiceCollection();
        foreach (var probe in probes)
            services.AddSingleton(probe);

        var provider = services.BuildServiceProvider();

        return new DeploymentPrerequisiteVerificationHostedService(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new FakeEnvironment(development ? Environments.Development : Environments.Production),
            NullLogger<DeploymentPrerequisiteVerificationHostedService>.Instance);
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

    private sealed class FakeEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
