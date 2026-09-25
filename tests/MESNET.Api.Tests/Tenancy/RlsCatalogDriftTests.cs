using MESNET.Api.Tests.Infrastructure;
using Shouldly;
using Xunit;
using static MESNET.Api.Tests.Tenancy.TenancyDatabase;

namespace MESNET.Api.Tests.Tenancy;

/// <summary>
/// Row-level security'nin SESSİZCE delinmediğini çalışan veritabanının kataloğundan doğrular (#318).
///
/// <para><b>Neden katalog, neden kaynak kod değil:</b> RLS'in en tehlikeli açıkları politikanın
/// içinde değil dışındadır — sonradan eklenen bir politika, bir view, bir fonksiyon ya da yanlış
/// rol filtreyi hiçbir davranış testi kırılmadan etkisiz bırakır. Elle uygulanan bir betik ya da
/// hotfix depoda görünmez; katalogda görünür (#195'in veritabanı karşılığı).</para>
///
/// <para>Ölçülen başlangıç durumu (25.09.2026): 52 kiracılı tablo, her birinde tek politika;
/// SECURITY DEFINER, view, materialized view yok; tek FK bileşik; PUBLIC'e tablo yetkisi yok.
/// Bu testler o durumu kilitler.</para>
/// </summary>
[Collection("api")]
public sealed class RlsCatalogDriftTests(ApiTestFixture fixture)
{
    private const string MartenPolicy = "marten_tenant_isolation";

    /// <summary>
    /// Kural 1 — permissive politikalar OR ile birleşir: aynı tabloya eklenecek bir
    /// <c>USING (true)</c> kiracı filtresini TÜMDEN kaldırır. Kiracılı tabloda tek permissive
    /// politika Marten'ınkidir.
    /// </summary>
    [Fact]
    public async Task Kiracili_tabloda_ek_permissive_politika_yok()
    {
        await using var conn = await OpenAsync(AdminConnectionString);
        var extra = await ColumnAsync(conn, $"""
            SELECT format('%s → %s', p.polrelid::regclass, p.polname)
            FROM pg_policy p
            WHERE p.polrelid IN ({TenantTablesSql}) AND p.polpermissive AND p.polname <> '{MartenPolicy}'
            ORDER BY 1
            """);

        extra.ShouldBeEmpty(
            "Kiracılı tabloda Marten dışında permissive politika var — permissive politikalar OR'lanır "
            + $"ve kiracı filtresini etkisiz bırakabilir: {string.Join(" | ", extra)}");
    }

    /// <summary>Kural 1 (tamamlayıcı) — her kiracılı tabloda kiracı politikası gerçekten var.</summary>
    [Fact]
    public async Task Her_kiracili_tabloda_kiraci_politikasi_var()
    {
        await using var conn = await OpenAsync(AdminConnectionString);
        var missing = await ColumnAsync(conn, $"""
            SELECT t.oid::regclass::text
            FROM ({TenantTablesSql}) t(oid)
            WHERE NOT EXISTS (SELECT 1 FROM pg_policy p WHERE p.polrelid = t.oid AND p.polname = '{MartenPolicy}')
            ORDER BY 1
            """);

        missing.ShouldBeEmpty($"Kiracı politikası olmayan kiracılı tablo: {string.Join(", ", missing)}");
    }

    /// <summary>
    /// Kural 2 (ters yön) — kiracısız (paylaşımlı/kimlik) tabloda kiracı politikası olmamalı:
    /// varsa belge yanlış sınıflandırılmıştır ve satırlar ya görünmez ya da yanlış süzülür.
    /// </summary>
    [Fact]
    public async Task Kiracisiz_tabloda_kiraci_politikasi_yok()
    {
        await using var conn = await OpenAsync(AdminConnectionString);
        var wrong = await ColumnAsync(conn, $"""
            SELECT format('%s → %s', p.polrelid::regclass, p.polname)
            FROM pg_policy p WHERE p.polrelid NOT IN ({TenantTablesSql}) ORDER BY 1
            """);

        wrong.ShouldBeEmpty($"Kiracı sütunu olmayan tabloda politika var: {string.Join(" | ", wrong)}");
    }

    /// <summary>
    /// Kural 3 — SECURITY DEFINER fonksiyon sahibinin yetkisiyle çalışır; sahip tablo sahibiyse
    /// ve RLS FORCE değilse filtreyi atlar. Uygulama şemalarında hiç olmamalı.
    /// </summary>
    [Fact]
    public async Task Uygulama_semalarinda_security_definer_fonksiyon_yok()
    {
        await using var conn = await OpenAsync(AdminConnectionString);
        var definers = await ColumnAsync(conn, $"""
            SELECT p.oid::regprocedure::text
            FROM pg_proc p JOIN pg_namespace n ON n.oid = p.pronamespace
            WHERE p.prosecdef AND {AppSchemaFilter}
            ORDER BY 1
            """);

        definers.ShouldBeEmpty($"SECURITY DEFINER fonksiyon RLS'i atlayabilir: {string.Join(", ", definers)}");
    }

    /// <summary>
    /// Kural 3 — normal view SAHİBİNİN yetkisiyle çalışır ve RLS'i atlar; materialized view'e
    /// RLS hiç uygulanmaz. View ancak <c>security_invoker = true</c> ile kabul edilir.
    /// </summary>
    [Fact]
    public async Task Uygulama_semalarinda_RLS_atlayan_view_yok()
    {
        await using var conn = await OpenAsync(AdminConnectionString);
        var views = await ColumnAsync(conn, $"""
            SELECT format('%s (%s)', c.oid::regclass, CASE c.relkind WHEN 'm' THEN 'materialized' ELSE 'view' END)
            FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace
            WHERE {AppSchemaFilter}
              AND (c.relkind = 'm'
                   OR (c.relkind = 'v' AND NOT COALESCE('security_invoker=true' = ANY(c.reloptions), false)
                                       AND NOT COALESCE('security_invoker=on' = ANY(c.reloptions), false)))
            ORDER BY 1
            """);

        views.ShouldBeEmpty(
            "RLS'i atlayan view/materialized view var — view'e security_invoker = true verin, "
            + $"kiracılı veriden materialized view üretmeyin: {string.Join(", ", views)}");
    }

    /// <summary>Kural 4 — uygulama rolleri RLS'i atlayamaz (#316 açılış kontrolünün test karşılığı).</summary>
    [Fact]
    public async Task Uygulama_rolleri_RLS_atlamaz()
    {
        await using var conn = await OpenAsync(AdminConnectionString);
        var roles = await ColumnAsync(conn, """
            SELECT format('%s super=%s bypassrls=%s', rolname, rolsuper, rolbypassrls)
            FROM pg_roles WHERE rolname IN ('mesnet_app', 'mesnet_owner') ORDER BY 1
            """);

        roles.ShouldBe(
            ["mesnet_app super=f bypassrls=f", "mesnet_owner super=f bypassrls=f"],
            "Uygulama rolleri yok ya da RLS'i atlıyor (#316).");
    }

    /// <summary>
    /// Kural 4 — PUBLIC'e tablo yetkisi yok; sahip rolün sonradan açacağı tablolar mesnet_app'e
    /// kendiliğinden yetki verir (varsayılan yetkiler tanımlı).
    /// </summary>
    [Fact]
    public async Task PUBLIC_tablo_yetkisi_yok_varsayilan_yetkiler_tanimli()
    {
        await using var conn = await OpenAsync(AdminConnectionString);
        var publicGrants = await ColumnAsync(conn, $"""
            SELECT format('%s %s', c.oid::regclass, a.privilege_type)
            FROM pg_class c JOIN pg_namespace n ON n.oid = c.relnamespace, aclexplode(c.relacl) a
            WHERE a.grantee = 0 AND {AppSchemaFilter}
            ORDER BY 1
            """);
        var defaults = await ColumnAsync(conn, """
            SELECT d.defaclobjtype::text FROM pg_default_acl d
            WHERE pg_get_userbyid(d.defaclrole) = 'mesnet_owner' ORDER BY 1
            """);

        publicGrants.ShouldBeEmpty($"PUBLIC'e tablo yetkisi verilmiş: {string.Join(", ", publicGrants)}");
        defaults.ShouldContain("r", "mesnet_owner için tablo varsayılan yetkisi yok — yeni tablolar mesnet_app'e kapalı doğar.");
    }

    /// <summary>
    /// Kural 5 — benzersizlik yan kanalı: kiracılı tabloda <c>tenant_id</c> içermeyen unique
    /// index, başka okulda aynı değerin varlığını <c>duplicate key</c> hatasıyla sızdırır.
    /// İzinliler gerekçeli ve adla listelenir; yenisi kırmızıdır.
    /// </summary>
    [Fact]
    public async Task Kiracili_tabloda_kiracisiz_unique_index_yalniz_gerekceli()
    {
        string[] allowed =
        [
            // Olay deposunun global sırası — değeri istemci belirlemez, sızdıracak bilgi yok.
            "shared.mt_events.pkey_mt_events_seq_id",
            // institutionId kiracının KENDİSİDİR (kiracı = okul, ADR-0003): tenant_id'ye eşdeğer.
            "coordination.mt_doc_branchstudentcountview.idx_branch_student_count_unique",
            "coordination.mt_doc_branchworkloadconfig.idx_branch_workload_config_unique",
            // studentId global benzersiz Guid — başka okulun değeriyle çakışamaz.
            "coordination.mt_doc_studenttermgrade.idx_student_term_grade_unique",
        ];

        await using var conn = await OpenAsync(AdminConnectionString);
        var found = await ColumnAsync(conn, $"""
            SELECT format('%s.%s.%s', n.nspname, t.relname, i.relname)
            FROM pg_index ix
            JOIN pg_class i ON i.oid = ix.indexrelid
            JOIN pg_class t ON t.oid = ix.indrelid
            JOIN pg_namespace n ON n.oid = t.relnamespace
            WHERE ix.indisunique AND t.oid IN ({TenantTablesSql})
              AND NOT EXISTS (SELECT 1 FROM pg_attribute a
                              WHERE a.attrelid = t.oid AND a.attname = 'tenant_id' AND a.attnum = ANY(ix.indkey))
            ORDER BY 1
            """);

        found.Except(allowed).ShouldBeEmpty(
            "Kiracılı tabloda tenant_id içermeyen yeni unique index — başka okulda aynı değerin varlığı "
            + "'duplicate key' ile sızar. TenancyScope.PerTenant kullanın ya da global benzersiz kimlik "
            + $"içerdiğini gerekçesiyle izin listesine yazın: {string.Join(", ", found.Except(allowed))}");
        allowed.Except(found).ShouldBeEmpty(
            $"İzin listesinde artık var olmayan index — satırı silin: {string.Join(", ", allowed.Except(found))}");
    }

    /// <summary>
    /// Kural 6 — referans bütünlüğü kontrolü politikaları GÖRMEZ: yalnız <c>id</c> üzerindeki bir
    /// FK başka okulun kaydına bağlanmayı kabul eder. Kiracılı tablodaki her FK tenant_id içerir.
    /// </summary>
    [Fact]
    public async Task Kiracili_tablodaki_her_foreign_key_kiraciyi_icerir()
    {
        await using var conn = await OpenAsync(AdminConnectionString);
        var bad = await ColumnAsync(conn, $"""
            SELECT format('%s → %s', c.conrelid::regclass, c.conname)
            FROM pg_constraint c
            WHERE c.contype = 'f' AND c.conrelid IN ({TenantTablesSql})
              AND NOT EXISTS (SELECT 1 FROM pg_attribute a
                              WHERE a.attrelid = c.conrelid AND a.attname = 'tenant_id' AND a.attnum = ANY(c.conkey))
            ORDER BY 1
            """);

        bad.ShouldBeEmpty($"tenant_id içermeyen FK başka okulun kaydına bağlanmayı kabul eder: {string.Join(", ", bad)}");
    }
}
