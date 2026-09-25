using MESNET.Api.Tests.Infrastructure;
using Npgsql;
using Shouldly;
using Xunit;

namespace MESNET.Api.Tests.Tenancy;

/// <summary>
/// Kiracı yalıtımının VERİTABANINDA da çalıştığını doğrular (#317).
///
/// <para><b>Neden API üzerinden değil:</b> row-level security'nin var olma sebebi Marten'ın
/// dışından geçen yollardır — ham ADO.NET, psql, rapor aracı. Bu testler API'nin kullandığı
/// rolle (<c>mesnet_app</c>) doğrudan bağlanır; politika eksikse ya da rol RLS'i atlıyorsa
/// burada görünür, HTTP'de görünmez (süzme doğru da yanlış da 200 döner).</para>
///
/// <para>Yazma denemeleri transaction içinde yapılır ve geri alınır — veri bırakılmaz.</para>
/// </summary>
[Collection("api")]
public sealed class RowLevelSecurityTests
{
    private const string Setting = "app.tenant_id";
    private const string Table = "enrollment.mt_doc_teacherprofile";

    private static string AppConnectionString => TenancyDatabase.AppConnectionString;
    private static string AdminConnectionString => TenancyDatabase.AdminConnectionString;

    [Fact]
    public async Task Kiracisiz_okuma_hata_verir_sessiz_bos_donmez()
    {
        await using var conn = await OpenAsync(AppConnectionString, pooling: false);

        var ex = await Should.ThrowAsync<PostgresException>(() => ScalarAsync(conn, null, $"SELECT count(*) FROM {Table}"));

        ex.MessageText.ShouldContain(Setting);
    }

    [Fact]
    public async Task A_kiracisinin_satirini_B_goremez()
    {
        await using var conn = await OpenAsync(AppConnectionString, pooling: false);
        await using var tx = await conn.BeginTransactionAsync(Ct);

        await SetTenantAsync(conn, tx, "rls-test-a");
        await ExecAsync(conn, tx, $"INSERT INTO {Table} (id, data, tenant_id) VALUES (gen_random_uuid(), '{{}}'::jsonb, 'rls-test-a')");
        (await CountAsync(conn, tx)).ShouldBe(1L, "Yazan kiracı kendi satırını görmeli.");

        await SetTenantAsync(conn, tx, "rls-test-b");
        (await CountAsync(conn, tx)).ShouldBe(0L, "B kiracısı A'nın satırını GÖRMEMELİ.");

        await tx.RollbackAsync(Ct);
    }

    [Fact]
    public async Task B_kiracisi_A_damgasiyla_yazamaz()
    {
        await using var conn = await OpenAsync(AppConnectionString, pooling: false);
        await using var tx = await conn.BeginTransactionAsync(Ct);

        await SetTenantAsync(conn, tx, "rls-test-b");

        var ex = await Should.ThrowAsync<PostgresException>(() => ExecAsync(conn, tx,
            $"INSERT INTO {Table} (id, data, tenant_id) VALUES (gen_random_uuid(), '{{}}'::jsonb, 'rls-test-a')"));

        ex.SqlState.ShouldBe(PostgresErrorCodes.InsufficientPrivilege);
    }

    /// <summary>
    /// Marten'ın RLS'i yalnız belge tablolarını korur; olay deposu ayrıca eklenir
    /// (<c>EventStoreRlsFeature</c>). Ölçülen ilk durum: 52 kiracılı tablodan 2'si açıktı.
    /// </summary>
    [Fact]
    public async Task Kiraci_sutunu_tasiyan_her_tablo_zorunlu_RLS_altinda()
    {
        await using var conn = await OpenAsync(AdminConnectionString, pooling: false);
        await using var cmd = new NpgsqlCommand("""
            SELECT string_agg(format('%s.%s', n.nspname, c.relname), ', ' ORDER BY 1)
            FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE c.relkind IN ('r', 'p')
              AND NOT (c.relrowsecurity AND c.relforcerowsecurity)
              AND EXISTS (SELECT 1 FROM pg_attribute a
                          WHERE a.attrelid = c.oid AND a.attname = 'tenant_id' AND NOT a.attisdropped)
            """, conn);

        var unprotected = await cmd.ExecuteScalarAsync(Ct) as string;

        unprotected.ShouldBeNull($"RLS'siz (ya da FORCE'suz) kiracılı tablo var: {unprotected}");
    }

    /// <summary>
    /// Havuza dönen bağlantı önceki okulun kiracısını taşımamalı: bir sonraki kullanıcı onu
    /// DEVRALIRSA başka okulun verisini görür.
    /// </summary>
    [Fact]
    public async Task Tek_baglantili_havuzda_kiraci_sizmaz()
    {
        var single = new NpgsqlConnectionStringBuilder(AppConnectionString)
        {
            MaxPoolSize = 1, MinPoolSize = 0, ApplicationName = "rls-pool-test",
        }.ConnectionString;

        await using (var first = await OpenAsync(single, pooling: true))
        {
            await ExecAsync(first, null, $"SELECT set_config('{Setting}', 'rls-test-a', false)");
        }

        await using var second = await OpenAsync(single, pooling: true);
        var inherited = await ScalarAsync(second, null, $"SELECT current_setting('{Setting}', true)");

        inherited.ShouldNotBe("rls-test-a", "Havuzdan alınan bağlantı önceki kiracıyı taşıyor.");
    }

    private static Task<NpgsqlConnection> OpenAsync(string cs, bool pooling) =>
        TenancyDatabase.OpenAsync(cs, pooling);

    private static Task SetTenantAsync(NpgsqlConnection conn, NpgsqlTransaction tx, string tenant) =>
        ExecAsync(conn, tx, $"SELECT set_config('{Setting}', '{tenant}', true)");

    private static async Task<long> CountAsync(NpgsqlConnection conn, NpgsqlTransaction tx) =>
        Convert.ToInt64(await ScalarAsync(conn, tx, $"SELECT count(*) FROM {Table} WHERE tenant_id LIKE 'rls-test-%'"));

    private static async Task ExecAsync(NpgsqlConnection conn, NpgsqlTransaction? tx, string sql)
    {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        await cmd.ExecuteNonQueryAsync();
    }

    private static async Task<object?> ScalarAsync(NpgsqlConnection conn, NpgsqlTransaction? tx, string sql)
    {
        await using var cmd = new NpgsqlCommand(sql, conn, tx);
        return await cmd.ExecuteScalarAsync();
    }
}
