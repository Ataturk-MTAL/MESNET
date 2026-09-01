using Marten;
using MESNET.Audit.Core.Entities;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MESNET.Audit.Application.Services;

/// <summary>
/// Yaşı geçen denetim satırlarını günlük olarak siler.
/// </summary>
/// <remarks>
/// <para><b>Süre yapılandırmadan gelir</b> (<c>Audit:RetentionMonths</c>), sabit kodlanmaz:
/// saklama süresi bir mevzuat kararıdır ve değiştiğinde yeni bir sürüm dağıtmak gerekmemeli.</para>
///
/// <para><b>Kiracı kiracı dolaşır.</b> Kiracı damgalı satırları silmek kiracı başına oturum
/// ister; kiracısız session yasaktır (<c>DefaultTenantUsageEnabled = false</c>).
/// <c>IDocumentSession</c> ENJEKTE EDİLMEZ — DI'dan gelen session kiracısızdır (proje kuralı:
/// arka plan işleri <c>IDocumentStore</c> alır).</para>
///
/// <para><b>Kaç satır silindiği kiracı başına loglanır.</b> Sessiz silme kabul edilemez: bir
/// denetim izinin kendi silinme kaydı olmadan çalışması, izin amacına aykırıdır.</para>
///
/// <para><c>platform</c> kiracısı da temizlenir — kurum üstü işlerin izi orada yaşar.</para>
/// </remarks>
public sealed class AuditRetentionService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<AuditRetentionService> logger) : BackgroundService
{
    private const int RunHourUtc = 3;
    private const int DefaultRetentionMonths = 24;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = CalculateNextRun(now);

            logger.LogInformation(
                "Denetim izi temizliği — sonraki çalışma: {NextRun:yyyy-MM-dd HH:mm} UTC ({Delay})",
                nextRun, nextRun - now);

            try
            {
                await Task.Delay(nextRun - now, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            try
            {
                await PurgeAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Bir günlük koşu patlarsa servis ÖLMEMELİ — yarın tekrar denenir.
                logger.LogError(ex, "Denetim izi temizliği başarısız oldu.");
            }
        }
    }

    /// <summary>Her gün 03:00 UTC — maaş (01:00) ve rapor (00:30) koşularının dışında.</summary>
    private static DateTime CalculateNextRun(DateTime now)
    {
        var today = new DateTime(now.Year, now.Month, now.Day, RunHourUtc, 0, 0, DateTimeKind.Utc);
        return now < today ? today : today.AddDays(1);
    }

    private async Task PurgeAsync(CancellationToken cancellationToken)
    {
        var months = configuration.GetValue<int?>("Audit:RetentionMonths") ?? DefaultRetentionMonths;
        if (months <= 0)
        {
            logger.LogWarning(
                "Denetim izi temizliği atlandı — Audit:RetentionMonths geçersiz: {Months}", months);
            return;
        }

        var cutoff = DateTimeOffset.UtcNow.AddMonths(-months);

        await using var scope = scopeFactory.CreateAsyncScope();
        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        var tenants = await scope.ServiceProvider
            .GetRequiredService<ITenantDirectory>()
            .GetActiveTenantsAsync(cancellationToken);

        // Kurum üstü işlerin izi platform kiracısında yaşar; kiracı dizininde görünmez.
        var targets = tenants.Append(TenantResolution.Platform).Distinct(StringComparer.Ordinal);

        foreach (var tenantId in targets)
        {
            try
            {
                await using var session = store.LightweightSession(tenantId);

                var silinecek = await session.Query<AuditEntry>()
                    .CountAsync(e => e.OccurredAt < cutoff, cancellationToken);

                if (silinecek == 0) continue;

                session.DeleteWhere<AuditEntry>(e => e.OccurredAt < cutoff);
                await session.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Denetim izi temizlendi — Kiracı: {TenantId}, Silinen: {Count}, Kesim: {Cutoff:yyyy-MM-dd}",
                    tenantId, silinecek, cutoff);
            }
            catch (Exception ex)
            {
                // Bir kiracının temizliği patlarsa diğerleri devam eder — tek okul yüzünden
                // bütün okulların izini büyütmek çok daha pahalıdır.
                logger.LogError(ex, "Denetim izi temizliği başarısız — Kiracı: {TenantId}", tenantId);
            }
        }
    }
}
