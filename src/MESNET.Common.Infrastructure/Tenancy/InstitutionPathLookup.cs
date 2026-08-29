using Marten;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MESNET.Common.Infrastructure.Tenancy;

/// <inheritdoc />
public sealed class InstitutionPathLookup(
    IDocumentStore store,
    IMemoryCache cache,
    ILogger<InstitutionPathLookup> logger) : IInstitutionPathLookup
{
    /// <summary>
    /// Kurum ağacı nadiren değişir; beş dakika <c>PermissionClaimsTransformation</c>'ın
    /// kapsam önbelleğiyle aynı süredir. Uzun tutulsaydı <c>rebuild-hierarchy</c> koştuktan
    /// sonra yeni yollar geç görünürdü.
    /// </summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    // Alias'sız `data`: Marten'in kendi `d.data` belirsizliği yok. Proje deseni
    // (GetBusinessClustersHandler, PermissionClaimsTransformation) ile aynı.
    private const string PathLookupSql = """
        SELECT data->>'path' AS path
        FROM institution.mt_doc_institution
        WHERE data->>'id' = @institutionId
        LIMIT 1
        """;

    public async Task<string?> GetPathAsync(Guid institutionId, CancellationToken cancellationToken = default)
    {
        if (institutionId == Guid.Empty) return null;

        var cacheKey = $"institution-path:{institutionId:D}";
        if (cache.TryGetValue(cacheKey, out string? cached))
            return cached;

        var path = await LookupAsync(institutionId, cancellationToken);

        // SONUÇSUZ ARAMA ÖNBELLEĞE ALINMAZ: geçiş ucu koşturulduğu anda yol doğar ve o
        // kurumun beş dakika daha yolsuz kalması için bir neden yoktur. Aynı gerekçe
        // PermissionClaimsTransformation'daki institution_path aramasında da yazılı.
        if (!string.IsNullOrEmpty(path))
            cache.Set(cacheKey, path, CacheDuration);

        return path;
    }

    private async Task<string?> LookupAsync(Guid institutionId, CancellationToken cancellationToken)
    {
        try
        {
            var conn = store.Storage.Database.CreateConnection();
            await conn.OpenAsync(cancellationToken);
            await using (conn)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = PathLookupSql;
                cmd.Parameters.Add(new NpgsqlParameter("institutionId", institutionId.ToString()));

                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                return result as string;
            }
        }
        catch (Exception ex)
        {
            // Arama patlarsa denetim satırı YOLSUZ yazılır — satırı tümden kaybetmekten iyidir.
            logger.LogWarning(ex, "Kurum yolu araması başarısız: {InstitutionId}", institutionId);
            return null;
        }
    }
}
