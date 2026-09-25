using System.Data.Common;
using Weasel.Core;
using Weasel.Core.Migrations;
using Weasel.Postgresql;
using DbCommandBuilder = Weasel.Core.DbCommandBuilder;

namespace MESNET.Common.Infrastructure.Tenancy;

/// <summary>
/// Olay deposu tablolarına (<c>mt_events</c>, <c>mt_streams</c>) kiracı yalıtım politikası (#317).
///
/// <para><b>Neden gerekli:</b> <c>UseRowLevelSecurity()</c> politikayı yalnız conjoined
/// <b>belge</b> tablolarına kurar. Olay deposu da conjoined'dır (<c>tenant_id</c> taşır) ama
/// Marten onu atlıyor — ölçüldü: 52 kiracılı tablodan 50'si korunuyor, açıkta kalan ikisi
/// olay tabloları. Bütün devamsızlık/sözleşme olay geçmişi veritabanı katmanında korumasız
/// kalırdı.</para>
///
/// <para><b>Marten'ın kendi nesnesinin aynısı:</b> <c>Marten.Storage.RlsPolicySchemaObject</c>
/// (Marten 9.39.1, MIT) <c>internal</c>'dır; SQL'i, katalog sorgusu ve politika adı birebir
/// ondan alındı. Aynı ad (<c>marten_tenant_isolation</c>) bilinçlidir: Marten ileride olay
/// tablolarını kendisi korursa iki taraf aynı nesneyi tanır, ikinci bir politika doğmaz.</para>
///
/// <para>Göç adımında (<c>resources setup</c>) şemanın parçası olarak kurulur/doğrulanır.</para>
/// </summary>
public sealed class EventStoreRlsPolicy : ISchemaObject
{
    public const string PolicyName = "marten_tenant_isolation";

    private readonly DbObjectName _table;
    private readonly string _settingName;

    public EventStoreRlsPolicy(string schema, string table, string settingName)
    {
        _table = new PostgresqlObjectName(schema, table, SchemaUtils.IdentifierUsage.General);
        _settingName = settingName;
        Identifier = new PostgresqlObjectName(
            schema, PostgresqlIdentifier.Shorten($"{table}_{PolicyName}"), SchemaUtils.IdentifierUsage.General);
    }

    public DbObjectName Identifier { get; }

    public void WriteCreateStatement(Migrator migrator, TextWriter writer)
    {
        var table = $"{_table.Schema}.{_table.Name}";
        writer.WriteLine($"ALTER TABLE {table} ENABLE ROW LEVEL SECURITY;");
        writer.WriteLine($"ALTER TABLE {table} FORCE ROW LEVEL SECURITY;");
        writer.WriteLine($"DROP POLICY IF EXISTS {PolicyName} ON {table};");
        writer.WriteLine($"CREATE POLICY {PolicyName} ON {table}");
        writer.WriteLine($"    USING (tenant_id = (select current_setting('{_settingName}')))");
        writer.WriteLine($"    WITH CHECK (tenant_id = (select current_setting('{_settingName}')));");
        writer.WriteLine();
    }

    public void WriteDropStatement(Migrator rules, TextWriter writer)
    {
        var table = $"{_table.Schema}.{_table.Name}";
        writer.WriteLine($"DROP POLICY IF EXISTS {PolicyName} ON {table};");
        writer.WriteLine($"ALTER TABLE {table} NO FORCE ROW LEVEL SECURITY;");
        writer.WriteLine($"ALTER TABLE {table} DISABLE ROW LEVEL SECURITY;");
        writer.WriteLine();
    }

    /// <summary>
    /// Politika yerinde mi: iki RLS bayrağı da açık ve USING/WITH CHECK aynı ayarı okuyor.
    /// Biri eksikse delta "Create" döner ve politika yeniden kurulur.
    /// </summary>
    public void ConfigureQueryCommand(DbCommandBuilder builder)
    {
        var pattern = $"%current_setting('{_settingName}'%";
        builder.Append("SELECT 1 FROM pg_class c JOIN pg_namespace n ON c.relnamespace = n.oid WHERE n.nspname = ");
        builder.AppendParameter(_table.Schema);
        builder.Append(" AND c.relname = ");
        builder.AppendParameter(_table.Name);
        builder.Append(" AND c.relrowsecurity = TRUE AND c.relforcerowsecurity = TRUE AND EXISTS (SELECT 1 FROM pg_policy p WHERE p.polrelid = c.oid AND p.polname = ");
        builder.AppendParameter(PolicyName);
        builder.Append(" AND pg_get_expr(p.polqual, p.polrelid) LIKE ");
        builder.AppendParameter(pattern);
        builder.Append(" AND pg_get_expr(p.polwithcheck, p.polrelid) LIKE ");
        builder.AppendParameter(pattern);
        builder.Append(");");
    }

    public async Task<ISchemaObjectDelta> CreateDeltaAsync(DbDataReader reader, CancellationToken ct = default)
    {
        var exists = await reader.ReadAsync(ct).ConfigureAwait(false);
        return new SchemaObjectDelta(this, exists ? SchemaPatchDifference.None : SchemaPatchDifference.Create);
    }

    public IEnumerable<DbObjectName> AllNames()
    {
        yield return Identifier;
    }
}

/// <summary>
/// Olay tablosu politikalarını taşıyan şema özelliği (#317).
///
/// <para><b>Neden ExtendedSchemaObjects DEĞİL:</b> Marten ek şema nesnelerini belge ve olay
/// tablolarından ÖNCE uygular; yeni veritabanında politika, henüz olmayan <c>mt_events</c>'e
/// ALTER TABLE deneyip bütün göçü geri aldırıyordu (ölçüldü: 42P01, iki koşuda da). Marten
/// dışı bir derlemeden <c>Storage.Add</c> ile kaydedilen özellik ise olay tablolarından SONRA
/// uygulanır (Marten 9.39.1 <c>StorageFeatures.AllActiveFeatures</c>).</para>
/// </summary>
public sealed class EventStoreRlsFeature(string schema) : FeatureSchemaBase("mesnet-event-store-rls", new PostgresqlMigrator())
{
    protected override IEnumerable<ISchemaObject> schemaObjects()
    {
        yield return new EventStoreRlsPolicy(schema, "mt_events", TenantRls.SettingName);
        yield return new EventStoreRlsPolicy(schema, "mt_streams", TenantRls.SettingName);
    }
}

/// <summary>Kiracı GUC'unun adı — Marten'ın politikası ile olay politikası AYNI adı okumalı.</summary>
public static class TenantRls
{
    public const string SettingName = "app.tenant_id";
}
