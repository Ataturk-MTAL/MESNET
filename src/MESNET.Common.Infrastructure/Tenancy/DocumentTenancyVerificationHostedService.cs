using Marten;
using MESNET.Common.Shared.Tenancy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MESNET.Common.Infrastructure.Tenancy;

/// <summary>
/// Açılışta <b>Marten'ın gerçekten tanıdığı</b> belge tiplerini
/// <see cref="DocumentTenancyMap"/> ile karşılaştırır (#149).
///
/// <para><b>Neden kaynak taraması yetmiyor:</b> <c>DocumentTenancyDriftTests</c>
/// <c>Schema.For&lt;T&gt;()</c> bildirimlerini <b>metin olarak</b> arar. Marten ise
/// bildirilmemiş bir tipi de <c>session.Store(x)</c> anında kendiliğinden kaydeder. Öyle bir
/// tip iki kontrolün de arasından geçer: testte görünmez, haritada yoktur ve kiracı damgası
/// almaz — yani çok okullu yapıda <b>sızıntı yüzeyi</b> olur. Bunu görebilecek tek yer,
/// mağazayı gerçekten kuran süreçtir.</para>
///
/// <para><b>Davranış ortama göre ayrılır</b> (<c>RealmVerificationHostedService</c> ile aynı
/// çizgi): Development'ta sapma <b>açılışı durdurur</b>; diğer ortamlarda
/// <c>LogCritical</c> — çalışan bir sistemi yapılandırma sapması yüzünden indirmek sapmadan
/// büyük zarar verebilir.</para>
///
/// <para>Marten'ın ve Wolverine'in kendi belge tipleri kapsam dışıdır; kontrol yalnız
/// <c>MESNET</c> derlemelerinden gelen tiplere bakar.</para>
/// </summary>
public sealed class DocumentTenancyVerificationHostedService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    ILogger<DocumentTenancyVerificationHostedService> logger) : IHostedService
{
    private const string AssemblyPrefix = "MESNET";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetService<IDocumentStore>();
        if (store is null)
        {
            logger.LogWarning("Belge kiracılık doğrulaması atlandı — Marten kayıtlı değil.");
            return Task.CompletedTask;
        }

        var unclassified = store.Options.AllKnownDocumentTypes()
            .Select(m => m.DocumentType)
            .Where(t => t.Assembly.GetName().Name?.StartsWith(AssemblyPrefix, StringComparison.Ordinal) == true)
            .Select(t => t.Name)
            .Where(name => !DocumentTenancyMap.All.ContainsKey(name))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

        if (unclassified.Count == 0)
        {
            logger.LogInformation(
                "Belge kiracılık sınıflandırması eksiksiz: {Count} tip.",
                DocumentTenancyMap.All.Count);
            return Task.CompletedTask;
        }

        var message =
            $"Kiracılık sınıflandırması eksik — Marten'a kayıtlı ama DocumentTenancyMap'te yok: "
            + $"{string.Join(", ", unclassified)}. Sınıflandırılmamış belge kiracı damgası ALMAZ; "
            + "çok okullu yapıda sorgu iki okulun satırını ayırt edemez. Tipi haritaya ekleyin "
            + "(#149).";

        if (environment.IsDevelopment())
            throw new InvalidOperationException(message);

        logger.LogCritical("{Message}", message);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
