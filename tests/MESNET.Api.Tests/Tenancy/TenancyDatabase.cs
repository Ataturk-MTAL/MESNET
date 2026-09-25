using Npgsql;

namespace MESNET.Api.Tests.Tenancy;

/// <summary>Veritabanına doğrudan bakan kiracılık testlerinin bağlantıları (#317, #318).</summary>
internal static class TenancyDatabase
{
    /// <summary>API'nin çalışma zamanı rolü (#316) — CI'daki compose kimlikleri; yerelde env ile.</summary>
    public static string AppConnectionString =>
        Environment.GetEnvironmentVariable("MESNET_APP_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=mesnet;Username=mesnet_app;Password=mesnet_app_dev";

    /// <summary>Katalog okuması için süper kullanıcı (RLS'i atlar — yalnız katalog okunur).</summary>
    public static string AdminConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__mesnet")
        ?? "Host=localhost;Port=5432;Database=mesnet;Username=mesnet;Password=mesnet_dev";

    /// <summary>Kiracıya ait tablo tanımı: <c>tenant_id</c> sütunu taşıyan tablo (conjoined).</summary>
    public const string TenantTablesSql = """
        SELECT c.oid FROM pg_class c
        WHERE c.relkind IN ('r', 'p')
          AND EXISTS (SELECT 1 FROM pg_attribute a
                      WHERE a.attrelid = c.oid AND a.attname = 'tenant_id' AND NOT a.attisdropped)
        """;

    /// <summary>
    /// Uygulama şemaları: sistem, public ve EKLENTİLERE AİT şemalar dışındakiler. Eklenti şeması
    /// (pg_cron'un <c>cron</c>'u, postgis_topology'nin <c>topology</c>'si) MESNET'in değildir — kendi
    /// politikaları ve PUBLIC yetkileri vardır. Ölçüldü: CI'daki kartoza kabı <c>POSTGRES_DB</c>
    /// ile kurulunca bu eklentileri veritabanına yüklüyor; dev'de yoklar.
    /// </summary>
    public const string AppSchemaFilter = """
        n.nspname NOT IN ('pg_catalog', 'information_schema', 'public') AND n.nspname !~ '^pg_'
        AND NOT EXISTS (SELECT 1 FROM pg_depend ed
                        WHERE ed.classid = 'pg_namespace'::regclass AND ed.objid = n.oid AND ed.deptype = 'e')
        """;

    /// <summary>Eklentiye ait olmayan tablo (<c>c</c> takma adı) — eklenti üyesi nesneler hariç.</summary>
    public const string NotExtensionMember = """
        NOT EXISTS (SELECT 1 FROM pg_depend ed
                    WHERE ed.classid = 'pg_class'::regclass AND ed.objid = c.oid AND ed.deptype = 'e')
        """;

    public static async Task<NpgsqlConnection> OpenAsync(string connectionString, bool pooling = false)
    {
        var conn = new NpgsqlConnection(
            new NpgsqlConnectionStringBuilder(connectionString) { Pooling = pooling }.ConnectionString);
        await conn.OpenAsync();
        return conn;
    }

    /// <summary>Sorgunun ilk sütununu satır satır okur.</summary>
    public static async Task<List<string>> ColumnAsync(NpgsqlConnection conn, string sql)
    {
        var rows = new List<string>();
        await using var cmd = new NpgsqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            rows.Add(reader.IsDBNull(0) ? "<null>" : reader.GetValue(0).ToString()!);
        return rows;
    }
}
