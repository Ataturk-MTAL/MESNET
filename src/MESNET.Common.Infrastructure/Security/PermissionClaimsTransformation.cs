using System.Security.Claims;
using System.Text.Json;
using MESNET.Common.Shared.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MESNET.Common.Infrastructure.Security;

/// <summary>
/// Keycloak paketinin rol dönüşümüne EK olarak çalışır.
/// Her istekte Marten'dan güncel UserAccount okur → roller + DirectPermissions → permission claim'lerine dönüştürür.
/// JWT stale claims problemini çözer: kullanıcı deaktive edilmişse permission eklenmez.
/// Ayrıca token'da institution_id yoksa DB'den staff eşleşmesiyle claim olarak ekler.
/// </summary>
public sealed class PermissionClaimsTransformation : IClaimsTransformation
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionClaimsTransformation> _logger;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Institution staff tablosundan keycloakId ile kurum ID'si bulan raw SQL.
    /// Modül entity referansı kullanmaz — schema izolasyonuna uyar.
    /// </summary>
    private const string InstitutionLookupSql = """
        SELECT data->>'id' AS institution_id
        FROM institution.mt_doc_institution
        WHERE EXISTS (
            SELECT 1 FROM jsonb_array_elements(data->'staff') AS s
            WHERE s->>'keycloakId' = @keycloakId
        )
        LIMIT 1
        """;

    public PermissionClaimsTransformation(
        IServiceProvider serviceProvider,
        IMemoryCache cache,
        ILogger<PermissionClaimsTransformation> logger)
    {
        _serviceProvider = serviceProvider;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        // İdempotency: zaten permission claim'leri eklenmişse tekrar ekleme
        if (principal.HasClaim(c => c.Type == "permissions"))
            return principal;

        // .NET JWT handler "sub" claim'ini ClaimTypes.NameIdentifier'a map eder
        var sub = principal.FindFirst("sub")?.Value
            ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(sub))
            return principal;

        // ── Institution claim enrichment ──
        // Token'da institution_id yoksa DB'den staff eşleşmesiyle bul ve claim olarak ekle
        await EnrichInstitutionClaimAsync(principal, sub);

        var cacheKey = $"user-permissions:{sub}";

        if (!_cache.TryGetValue(cacheKey, out PermissionCacheEntry? entry))
        {
            entry = await LoadFromMartenAsync(sub);

            if (entry is not null)
            {
                _cache.Set(cacheKey, entry, CacheDuration);
            }
        }

        // Permission claim'lerini ekle
        var identity = principal.Identity as ClaimsIdentity;
        if (identity is null)
            return principal;

        IEnumerable<string> allPermissions;

        if (entry is not null)
        {
            // Kullanıcı deaktive edilmişse permission eklenmez → erişim engellenir
            if (!entry.IsEnabled)
            {
                _logger.LogWarning("Deaktive kullanıcı erişim denemesi: {KeycloakUserId}", sub);
                return principal;
            }

            // Marten'dan gelen roller + doğrudan atanan permission'lar
            var rolePermissions = RolePermissionMap.GetPermissionsForRoles(entry.Roles);
            var permSet = new HashSet<string>(rolePermissions, StringComparer.OrdinalIgnoreCase);
            foreach (var dp in entry.DirectPermissions)
                permSet.Add(dp);

            allPermissions = permSet;
        }
        else
        {
            // UserAccount Marten'da henüz yok — JWT token'daki realm_access rollerinden
            // permission'ları türet. İlk login veya seed öncesi kullanıcılar için fallback.
            var tokenRoles = ExtractRealmRolesFromToken(principal);
            if (tokenRoles.Count == 0)
                return principal;

            _logger.LogInformation(
                "UserAccount bulunamadı, JWT realm roles'dan permission türetiliyor: {KeycloakUserId}, Roles={Roles}",
                sub, string.Join(", ", tokenRoles));

            allPermissions = RolePermissionMap.GetPermissionsForRoles(tokenRoles);
        }

        foreach (var permission in allPermissions)
        {
            identity.AddClaim(new Claim("permissions", permission));
        }

        return principal;
    }

    private async Task<PermissionCacheEntry?> LoadFromMartenAsync(string keycloakUserId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var provider = scope.ServiceProvider.GetService<IUserPermissionProvider>();

            if (provider is null)
            {
                _logger.LogDebug("IUserPermissionProvider henüz kayıtlı değil — permission dönüşümü atlanıyor.");
                return null;
            }

            var info = await provider.GetUserPermissionInfoAsync(keycloakUserId);
            if (info is null) return null;

            return new PermissionCacheEntry(info.IsEnabled, info.Roles, info.DirectPermissions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UserAccount yüklenirken hata: {KeycloakUserId}", keycloakUserId);
            return null;
        }
    }

    /// <summary>
    /// Cache'den kullanıcı permission bilgisini invalidate eder.
    /// ChangeUserRolesHandler ve ChangeUserPermissionsHandler tarafından çağrılır.
    /// </summary>
    public static void InvalidateCache(IMemoryCache cache, string keycloakUserId)
    {
        cache.Remove($"user-permissions:{keycloakUserId}");
        cache.Remove($"user-institution:{keycloakUserId}");
    }

    /// <summary>
    /// Token'da institution_id claim'i yoksa DB'den staff eşleşmesiyle bulup claim olarak ekler.
    /// Raw SQL kullanır — Institution modülüne proje referansı gerekmez.
    /// </summary>
    private async Task EnrichInstitutionClaimAsync(ClaimsPrincipal principal, string keycloakUserId)
    {
        if (principal.HasClaim(c => c.Type == "institution_id"))
            return;

        var cacheKey = $"user-institution:{keycloakUserId}";

        if (!_cache.TryGetValue(cacheKey, out string? institutionId))
        {
            institutionId = await LookupInstitutionIdAsync(keycloakUserId);
            _cache.Set(cacheKey, institutionId ?? string.Empty, CacheDuration);
        }

        if (!string.IsNullOrEmpty(institutionId))
        {
            (principal.Identity as ClaimsIdentity)?.AddClaim(new Claim("institution_id", institutionId));
        }
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
                cmd.CommandText = InstitutionLookupSql;
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
    /// JWT token'daki realm_access claim'inden roller çıkarır.
    /// Claim değeri JSON: {"roles":["InstitutionManager","Teacher"]}
    /// </summary>
    private static IReadOnlyList<string> ExtractRealmRolesFromToken(ClaimsPrincipal principal)
    {
        // Önce ClaimTypes.Role (KeycloakRolesClaimsTransformation tarafından eklenir)
        var roleClaims = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (roleClaims.Count > 0)
            return roleClaims;

        // Fallback: realm_access JSON claim'ini parse et
        var realmAccessClaim = principal.FindFirst("realm_access")?.Value;
        if (string.IsNullOrEmpty(realmAccessClaim))
            return [];

        try
        {
            using var doc = JsonDocument.Parse(realmAccessClaim);
            if (doc.RootElement.TryGetProperty("roles", out var rolesElement))
            {
                return rolesElement.EnumerateArray()
                    .Select(r => r.GetString()!)
                    .Where(r => !string.IsNullOrEmpty(r))
                    .ToList();
            }
        }
        catch (JsonException)
        {
            // Geçersiz JSON — boş dön
        }

        return [];
    }

    private sealed record PermissionCacheEntry(
        bool IsEnabled,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> DirectPermissions);
}
