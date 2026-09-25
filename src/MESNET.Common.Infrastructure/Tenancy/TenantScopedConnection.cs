using Marten;
using Npgsql;

namespace MESNET.Common.Infrastructure.Tenancy;

/// <summary>
/// Kiracıya ait tabloyu ham SQL ile okumak için bağlantı (#317).
///
/// <para><b>Neden gerekli:</b> row-level security politikası satırları
/// <c>current_setting('app.tenant_id')</c> ile süzer ve Marten bu ayarı YALNIZ session
/// bağlantısında kurar. <c>Storage.Database.CreateConnection()</c> ile açılan bağlantıda ayar
/// ya hiç yoktur (hata) ya da önceki session'ın bıraktığı boş dizedir — ölçüldü: tek bağlantılı
/// havuzda bir session'dan sonra ham bağlantı ayarı <c>''</c> görüyor ve kiracılı tablo
/// <b>hatasız 0 satır</b> dönüyor. İşletme kümeleme haritası böyle sessizce boşalırdı.</para>
///
/// <para><b>Kiracı yalnız transaction'a kurulur</b> (<c>set_config(..., true)</c>): transaction
/// bitince silinir, bağlantı havuza temiz döner ve sonraki kullanıcı bu okulun kiracısını
/// DEVRALAMAZ.</para>
///
/// <para>Kiracı boşsa istisna: sessiz boş sonuç yerine yüksek sesle hata
/// (<c>DefaultTenantUsageEnabled = false</c> ile aynı ilke).</para>
/// </summary>
public sealed class TenantScopedConnection : IAsyncDisposable
{
    private TenantScopedConnection(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        Connection = connection;
        Transaction = transaction;
    }

    public NpgsqlConnection Connection { get; }

    public NpgsqlTransaction Transaction { get; }

    public static async Task<TenantScopedConnection> OpenAsync(
        IDocumentStore store, string? tenantId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new InvalidOperationException(
                "Kiracıya ait tablo ham SQL ile kiracısız okunamaz. Row-level security açıkken "
                + "sonuç hata değil BOŞ dönerdi (#317).");
        }

        var connection = store.Storage.Database.CreateConnection();
        try
        {
            await connection.OpenAsync(cancellationToken);
            var transaction = await connection.BeginTransactionAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT set_config(@name, @tenant, true)";
            command.Parameters.AddWithValue("name", TenantRls.SettingName);
            command.Parameters.AddWithValue("tenant", tenantId);
            await command.ExecuteNonQueryAsync(cancellationToken);

            return new TenantScopedConnection(connection, transaction);
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    /// <summary>Komut bu bağlantının transaction'ına bağlı yaratılır — ayar yalnız orada geçerli.</summary>
    public NpgsqlCommand CreateCommand(string sql)
    {
        var command = Connection.CreateCommand();
        command.Transaction = Transaction;
        command.CommandText = sql;
        return command;
    }

    public async ValueTask DisposeAsync()
    {
        // Salt okuma: commit ile rollback aynı sonucu verir; rollback yazma sızdırmaz.
        await Transaction.DisposeAsync();
        await Connection.DisposeAsync();
    }
}
