using MESNET.Api.Tests.Infrastructure;
using Npgsql;
using Shouldly;
using Xunit;

namespace MESNET.Api.Tests.Tenancy;

/// <summary>
/// Kiracı damgasının veritabanında gerçekten durduğunu doğrular (#149, ADR-0003 adım 5).
///
/// <para><b>Neden HTTP üzerinden bakılamaz:</b> kiracılık bir <i>süzme</i> mekanizmasıdır —
/// doğru çalıştığında da yanlış çalıştığında da API 200 döner. Damgasız bir satır, onu yazan
/// kiracıya sorulduğunda görünmez; yalnız <b>başka</b> bir kiracı sorduğunda ortaya çıkar. Tek
/// güvenilir gözlem noktası tabloların kendisidir.</para>
///
/// <para><b>Neden kalıcı, tek seferlik göç kontrolü değil:</b> bu testler göçün değil
/// <i>yapılandırmanın</i> nöbetçisidir. <c>DocumentTenancyPolicy</c> sessizce devre dışı
/// kalırsa (haritadan bir tip düşer, politika kaydı silinir) yeni yazılan satırlar damgasız
/// doğar ve hiçbir birim testi bunu göremez.</para>
/// </summary>
[Collection("api")]
public sealed class TenantStampIntegrityTests(ApiTestFixture fixture)
{
    /// <summary>CI'daki compose kimlikleri; yerelde env ile geçilir.</summary>
    private static string ConnectionString =>
        Environment.GetEnvironmentVariable("ConnectionStrings__mesnet")
        ?? "Host=localhost;Port=5432;Database=mesnet;Username=mesnet;Password=mesnet_dev";

    /// <summary>
    /// Marten'ın kiracısız satırlar için kullandığı kova. Kapı açıldıktan sonra
    /// (<c>DefaultTenantUsageEnabled = false</c>) buraya satır <b>düşemez</b>: düşmüşse ya göç
    /// yarım kalmıştır ya da bir yazma yolu kiracıyı hiç taşımamıştır.
    /// </summary>
    private const string DefaultTenant = "*DEFAULT*";

    [Fact]
    public async Task Hicbir_satir_varsayilan_kiracida_kalmaz()
    {
        await using var conn = await OpenAsync();

        var offenders = new List<string>();
        foreach (var (schema, table) in await TenantStampedTablesAsync(conn))
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT count(*) FROM \"{schema}\".\"{table}\" WHERE tenant_id = @t";
            cmd.Parameters.AddWithValue("t", DefaultTenant);

            var count = Convert.ToInt64(await cmd.ExecuteScalarAsync());
            if (count > 0)
                offenders.Add($"{schema}.{table} → {count} satır");
        }

        offenders.ShouldBeEmpty(
            "Varsayılan kiracıda satır kalmış. Bu satırlar hiçbir okulun sorgusunda görünmez ve "
            + "silinmedikleri için sessizce birikirler. Göç eksikse damgalama betiğini çalıştırın; "
            + $"yeni yazılıyorlarsa o yazma yolu kiracıyı taşımıyordur. İhlaller: {string.Join(" | ", offenders)}");
    }

    /// <summary>
    /// Olay akışları da kiracıya aittir: <c>mt_streams</c> birincil anahtarı
    /// <c>(tenant_id, id)</c> olmalıdır. Tek sütunlu kalırsa iki okul aynı akış kimliğini
    /// paylaşamaz ve <c>Events.TenancyStyle</c> ayarı hiç uygulanmamış demektir.
    /// </summary>
    [Fact]
    public async Task Olay_akislari_kiraciya_aittir()
    {
        await using var conn = await OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT string_agg(a.attname, ',' ORDER BY k.ord)
            FROM pg_constraint c
            JOIN LATERAL unnest(c.conkey) WITH ORDINALITY AS k(attnum, ord) ON TRUE
            JOIN pg_attribute a ON a.attrelid = c.conrelid AND a.attnum = k.attnum
            WHERE c.conrelid = 'shared.mt_streams'::regclass AND c.contype = 'p'
            """;

        var columns = (await cmd.ExecuteScalarAsync())?.ToString();

        columns.ShouldBe("tenant_id,id",
            "shared.mt_streams birincil anahtarı (tenant_id, id) olmalı. Değilse "
            + "Events.TenancyStyle = Conjoined uygulanmamıştır ve olaylar okullar arasında ayrışmaz.");
    }

    /// <summary>
    /// Sınıflandırmanın iki yönü de gözlenebilir olmalı: kiracıya ait bir belge damga
    /// <b>taşımalı</b>, kimlik/paylaşımlı belge <b>taşımamalı</b>. Tek yönlü kontrol
    /// (yalnız "damga var mı") politikanın toptan <c>AllDocumentsAreMultiTenanted()</c>'a
    /// kaymasını göremezdi.
    ///
    /// <para>Marten <c>AutoCreate</c> ile <b>tembeldir</b>: belge tablosunu ilk kullanımda yaratır.
    /// Bu yüzden şemaya bakmadan önce ilgili uç çağrılır — yoksa tablo daha var olmayabilir ve
    /// kontrol, hiçbir şey doğrulamadan yeşil geçerdi. (Boş veritabanında ölçüldü: API sağlıklı
    /// açılıyor ama yalnız iki tabloda <c>tenant_id</c> var, çünkü belge tabloları henüz yok.)</para>
    /// </summary>
    [Theory]
    [InlineData("/api/students", "enrollment", "mt_doc_studentprofile", true, "öğrenci kaydı okulundur")]
    [InlineData("/api/contracts", "contract", "mt_doc_internshipcontract", true, "sözleşme okulundur")]
    [InlineData("/api/attendance", "attendance", "mt_doc_attendancerecord", true, "devamsızlık okulundur")]
    [InlineData("/api/businesses", "business", "mt_doc_business", false, "işletme kataloğu okullar arası paylaşımlıdır")]
    [InlineData("/api/security/users", "security", "mt_doc_useraccount", false, "kimlik katmanı kiracıyı çözmek için okunur")]
    [InlineData("/api/institutions", "institution", "mt_doc_institution", false, "okul kaydı kiracının kendisidir")]
    public async Task Belge_siniflandirmasi_tabloya_yansir(
        string warmupPath, string schema, string table, bool shouldBeStamped, string reason)
    {
        // Tabloyu var ettir; yanıtın içeriği önemsiz, sorgunun çalışmış olması yeterli.
        (await fixture.Client.GetAsync($"{warmupPath}?page=1&pageSize=1"))
            .IsSuccessStatusCode.ShouldBeTrue($"{warmupPath} çağrılamadı; tablo doğrulaması anlamsız olurdu.");

        await using var conn = await OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT count(*) FROM information_schema.columns
            WHERE table_schema = @s AND table_name = @t AND column_name = 'tenant_id'
            """;
        cmd.Parameters.AddWithValue("s", schema);
        cmd.Parameters.AddWithValue("t", table);

        var hasStamp = Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;

        hasStamp.ShouldBe(shouldBeStamped,
            $"{schema}.{table} damga durumu beklenenden farklı — {reason}. Damgayı sonradan "
            + "eklemek/çıkarmak tablo yeniden inşası ve veri göçü demektir.");
    }

    private static async Task<NpgsqlConnection> OpenAsync()
    {
        var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        return conn;
    }

    private static async Task<List<(string Schema, string Table)>> TenantStampedTablesAsync(
        NpgsqlConnection conn)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT table_schema, table_name FROM information_schema.columns
            WHERE column_name = 'tenant_id'
              AND table_schema NOT IN ('pg_catalog', 'information_schema')
            ORDER BY table_schema, table_name
            """;

        var tables = new List<(string, string)>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            tables.Add((reader.GetString(0), reader.GetString(1)));

        return tables;
    }
}
