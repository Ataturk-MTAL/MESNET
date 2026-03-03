using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MESNET.Common.Infrastructure.Security;

/// <summary>
/// JWT token'da institution_id claim'i olmayan kullanıcılar için
/// Institution staff eşleşmesi yaparak claim olarak ekler.
/// Keycloak token'a institution_id yazılamayan durumlarda (admin, seed öncesi)
/// DB fallback sağlar. Tüm endpoint'ler claim'den institution_id okuyabilir.
/// Raw SQL kullanır — Institution modülüne proje referansı gerekmez.
/// </summary>
public sealed class InstitutionClaimsTransformation : IClaimsTransformation
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<InstitutionClaimsTransformation> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Institution staff tablosundan keycloakId ile kurum ID'si bulan raw SQL.
    /// Modül entity referansı kullanmaz — schema izolasyonuna uyar.
    /// staff JSON array içinde keycloakId ile eşleşen ilk institution'ın id'sini döner.
    /// </summary>
    private const string LookupSql = """
        SELECT data->>'id' AS institution_id
        FROM institution.mt_doc_institution
        WHERE EXISTS (
            SELECT 1 FROM jsonb_array_elements(data->'staff') AS s
            WHERE s->>'keycloakId' = @keycloakId
        )
        LIMIT 1
        """;

    public InstitutionClaimsTransformation(
        IServiceProvider serviceProvider,
        IMemoryCache cache,
        ILogger<InstitutionClaimsTransformation> logger)
    {
        _serviceProvider = serviceProvider;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // Token'da zaten institution_id varsa bir şey yapma
        if (principal.HasClaim(c => c.Type == "institution_id"))
            return principal;

        var sub = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(sub))
            return principal;

        var cacheKey = $"user-institution:{sub}";

        if (!_cache.TryGetValue(cacheKey, out string? institutionId))
        {
            institutionId = await LookupInstitutionIdAsync(sub);

            // Cache — null bile olsa (boş string olarak) cache'le, her request'te DB'ye gitmesin
            _cache.Set(cacheKey, institutionId ?? string.Empty, CacheDuration);
        }

        if (!string.IsNullOrEmpty(institutionId))
        {
            var identity = principal.Identity as ClaimsIdentity;
            identity?.AddClaim(new Claim("institution_id", institutionId));
        }

        return principal;
    }

    private async Task<string?> LookupInstitutionIdAsync(string keycloakUserId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var store = scope.ServiceProvider.GetService<Marten.IDocumentStore>();
            if (store is null)
                return null;

            var conn = store.Storage.Database.CreateConnection();
            await conn.OpenAsync();
            await using (conn)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = LookupSql;
                cmd.Parameters.Add(new NpgsqlParameter("keycloakId", keycloakUserId));

                var result = await cmd.ExecuteScalarAsync();
                if (result is string id && !string.IsNullOrEmpty(id))
                {
                    _logger.LogDebug(
                        "Institution claim eklendi (DB fallback): {KeycloakUserId} → {InstitutionId}",
                        keycloakUserId, id);
                    return id;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Institution claim lookup hatası: {KeycloakUserId}", keycloakUserId);
        }

        return null;
    }

    /// <summary>
    /// Cache invalidation — staff değişikliklerinde çağrılır.
    /// </summary>
    public static void InvalidateCache(IMemoryCache cache, string keycloakUserId)
    {
        cache.Remove($"user-institution:{keycloakUserId}");
    }
}
