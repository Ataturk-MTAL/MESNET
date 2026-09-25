using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MESNET.Common.Infrastructure.Database;

/// <summary>
/// Açılışta API'nin veritabanına hangi rolle bağlandığını ölçer (#316).
///
/// <para><b>Neden çalışma zamanında:</b> bağlantı dizesi yapılandırmadan gelir; depodaki örnek
/// doğru olsa bile çalışan ortam süper kullanıcıyla bağlanabilir ve bunu hiçbir birim testi
/// göremez (#195'teki realm doğrulamasıyla aynı gerekçe).</para>
///
/// <para><b>Davranış ortama göre ayrılır</b> (<c>RealmVerificationHostedService</c> ile aynı):
/// Development'ta açılış DURUR, diğer ortamlarda <c>LogCritical</c>. Veritabanına ulaşılamaması
/// ihlal sayılmaz — o durum zaten başka yerde gürültülü biçimde başarısız olur.</para>
/// </summary>
public sealed class DatabaseRoleVerificationHostedService(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    ILogger<DatabaseRoleVerificationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var store = scope.ServiceProvider.GetService<IDocumentStore>();
        if (store is null)
        {
            logger.LogWarning("Veritabanı rolü doğrulaması atlandı — Marten kayıtlı değil.");
            return;
        }

        DatabaseRole role;
        try
        {
            role = await ReadCurrentRoleAsync(store, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex,
                "Veritabanı rolü doğrulaması atlandı — veritabanına ulaşılamadı. "
                + "Bu bir ihlal bulgusu DEĞİLDİR; rol doğrulanmamış durumdadır.");
            return;
        }

        var violation = DatabaseRolePolicy.Violation(role);
        if (violation is null)
        {
            logger.LogInformation("Veritabanı rolü doğrulandı: {Role} (RLS'i atlamıyor).", role.Name);
            return;
        }

        if (environment.IsDevelopment())
            throw new InvalidOperationException(violation);

        logger.LogCritical("{Violation}", violation);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static async Task<DatabaseRole> ReadCurrentRoleAsync(IDocumentStore store, CancellationToken ct)
    {
        await using var connection = store.Storage.Database.CreateConnection();
        await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT rolname, rolsuper, rolbypassrls FROM pg_roles WHERE rolname = current_user";

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
            throw new InvalidOperationException("current_user pg_roles'ta bulunamadı.");

        return new DatabaseRole(reader.GetString(0), reader.GetBoolean(1), reader.GetBoolean(2));
    }
}
