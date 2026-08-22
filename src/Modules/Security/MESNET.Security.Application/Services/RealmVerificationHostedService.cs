using MESNET.Common.Shared.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MESNET.Security.Application.Services;

/// <summary>
/// Açılışta çalışan Keycloak realm'ini depodaki tanımla karşılaştırır (#195).
///
/// <para><b>Neden açılışta:</b> realm import tek seferliktir. Depoya eklenen bir güvenlik ayarı
/// mevcut bir kaba hiç ulaşmaz ve bunu hiçbir birim testi göremez — testler depodaki dosyayı
/// okur, çalışan realm'i değil. Sapmayı görebilecek tek yer, realm'e gerçekten bağlanan süreçtir.</para>
///
/// <para><b>Davranış ortama göre ayrılır:</b></para>
/// <list type="bullet">
///   <item>Development: sapma varsa <b>açılış durur</b>. Görülmeyen uyarı, uyarı değildir;
///     #190'da sessiz başarı 41 koşu boyunca fark edilmemişti.</item>
///   <item>Diğer ortamlar: <c>LogCritical</c> — çalışan bir sistemi yapılandırma sapması yüzünden
///     indirmek, sapmanın kendisinden büyük zarar verebilir.</item>
/// </list>
///
/// <para><b>Ulaşılamamak sapma değildir.</b> Keycloak henüz ayağa kalkmadıysa ya da ağ kopuksa
/// kontrol atlanır ve uyarı yazılır — hiçbir ortamda açılış bu yüzden durmaz. Aksi hâlde bu
/// kontrol, Keycloak gecikmesini uygulama arızasına çevirirdi.</para>
/// </summary>
public sealed class RealmVerificationHostedService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    ILogger<RealmVerificationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // IKeycloakAdminService Scoped — hosted service Singleton'dır, kendi kapsamını açar.
        using var scope = scopeFactory.CreateScope();
        var keycloak = scope.ServiceProvider.GetRequiredService<IKeycloakAdminService>();

        var snapshotResult = await keycloak.GetRealmSnapshotAsync(cancellationToken);
        if (snapshotResult.IsFailure)
        {
            logger.LogWarning(
                "Realm doğrulaması atlandı — Keycloak'a ulaşılamadı: {Error}. "
                + "Bu bir sapma bulgusu DEĞİLDİR; realm doğrulanmamış durumdadır.",
                snapshotResult.Error.Description);
            return;
        }

        var snapshot = snapshotResult.Value;

        if (snapshot.UnreadableFields is { Count: > 0 } unreadable)
        {
            logger.LogWarning(
                "Realm ayarlarının bir kısmı okunamadı, o alanlar doğrulanmadı: {Fields}. "
                + "mesnet-api servis hesabının realm-management yetkilerini kontrol edin.",
                string.Join(", ", unreadable));
        }

        var drifts = RealmInvariants.Verify(snapshot);
        if (drifts.Count == 0)
        {
            logger.LogInformation("Realm doğrulandı — depodaki tanımla uyumlu.");
            return;
        }

        var rapor =
            $"Çalışan Keycloak realm'i depodaki tanımdan SAPMIŞ ({drifts.Count} ayar):"
            + Environment.NewLine + RealmInvariants.Describe(drifts) + Environment.NewLine
            + "Realm import tek seferliktir; depoya sonradan eklenen ayarlar mevcut kaba ulaşmaz. "
            + "Kalıcı çözüm: realm'i yeniden import edin (dev verisi gider) ya da yukarıdaki "
            + "düzeltmeleri Admin API ile uygulayın (#195).";

        if (environment.IsDevelopment())
            throw new InvalidOperationException(rapor);

        logger.LogCritical("{Rapor}", rapor);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
