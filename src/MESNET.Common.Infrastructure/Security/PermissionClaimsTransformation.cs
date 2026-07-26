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

    /// <summary>
    /// Personel kaydındaki alan (branş) kodlarını keycloakId ile bulan raw SQL (#126).
    /// Kullanıcı birden çok alandan sorumlu olabilir → satır kümesi döner.
    ///
    /// <para>Branş kodu olmayan personel (okul müdürü, müdür yardımcısı) hiç satır üretmez —
    /// bu beklenen normal durumdur, eksik veri değildir. Sorgu asla kod uydurmaz.</para>
    ///
    /// <para>Institution modülüne proje referansı kullanmaz — schema izolasyonuna uyar.</para>
    /// </summary>
    private const string BranchCodesLookupSql = """
        SELECT DISTINCT s->>'branchCode' AS branch_code
        FROM institution.mt_doc_institution,
             LATERAL jsonb_array_elements(data->'staff') AS s
        WHERE s->>'keycloakId' = @keycloakId
          AND COALESCE(s->>'branchCode', '') <> ''
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

        // ── Alan (branş) kapsamı claim enrichment (#126) ──
        // Sıra: token claim → kullanıcı kaydındaki BranchCodes → personel kaydı yedeği.
        // Kayıt sırasında girilen bilgi birincil kaynaktır; personel kaydı yalnız mevcut
        // kullanıcılar için geçiş adımıdır. Hiçbiri yoksa claim eklenmez — bu geçerli bir
        // durumdur (müdür/müdür yardımcısı hiçbir alana bağlı değildir).
        await EnrichBranchCodesClaimAsync(principal, sub, entry?.BranchCodes);

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

            return new PermissionCacheEntry(
                info.IsEnabled, info.Roles, info.DirectPermissions, info.BranchCodes);
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
        cache.Remove($"user-branch-codes:{keycloakUserId}");
    }

    /// <summary>
    /// <c>branch_codes</c> claim'ini çözer (#126). Öncelik sırası:
    ///
    /// <list type="number">
    ///   <item><b>Kullanıcı kaydı</b> (<c>UserAccount.BranchCodes</c>) — <b>OTORİTERDİR</b>.
    ///         Doluysa token'dan gelen <c>branch_codes</c> claim'leri <b>atılır</b> ve yerine
    ///         kayıttaki değerler konur</item>
    ///   <item><b>Token claim'i</b> — yalnız kullanıcı kaydında alan YOKKEN kabul edilir
    ///         (#126 öncesi oluşturulmuş, kaydı henüz doldurulmamış kullanıcılar)</item>
    ///   <item><b>Personel kaydı yedeği</b> — kurum personel belgesindeki
    ///         <c>staff[].branchCode</c>; geçiş adımıdır, birincil yol DEĞİLDİR</item>
    /// </list>
    ///
    /// <para>Üçü de boşsa claim eklenmez. Bu bir hata değildir: müdür ve müdür yardımcısı
    /// hiçbir alana bağlı değildir ve muafiyet izniyle çalışır.</para>
    ///
    /// <para><b>GÜVENLİK — bu sırayı TERS ÇEVİRMEYİN.</b> "Token zaten Keycloak'tan geliyor,
    /// imzalı, güvenilir" düşüncesi burada YANLIŞTIR. <c>branch_codes</c> Keycloak'ta
    /// <i>unmanaged</i> bir kullanıcı özniteliğidir; realm politikası yanlışlıkla
    /// <c>ENABLED</c>'a çekilirse (ya da başka bir realm/ortam öyle kurulursa) kullanıcı
    /// varsayılan <c>manage-account</c> rolüyle kendi Account konsolundan/REST API'sinden
    /// <b>kendi özniteliğini yazabilir</b>. O durumda EET alan şefi kendine <c>MTT</c> ekleyip
    /// #126'nın engellemek için var olduğu şeyi — başka alanın saat dağıtımını ezmeyi —
    /// yapabilirdi. Token imzalı olması içeriğin <b>kullanıcı tarafından belirlenmediği</b>
    /// anlamına gelmez.</para>
    ///
    /// <para>Realm tarafında politika <c>ADMIN_EDIT</c>'tir (kullanıcı ne görür ne yazar);
    /// buradaki kontrol o yapılandırmaya <b>bağımlı olmayan</b> ikinci katmandır. İkisinden
    /// biri kaldırılırsa açık geri gelir.</para>
    /// </summary>
    private async Task EnrichBranchCodesClaimAsync(
        ClaimsPrincipal principal, string keycloakUserId, IReadOnlyList<string>? accountBranchCodes)
    {
        if (principal.Identity is not ClaimsIdentity identity)
            return;

        // (1) Kullanıcı kaydı OTORİTERDİR — token'dan geleni ezer.
        if (accountBranchCodes is { Count: > 0 })
        {
            RemoveBranchCodeClaims(principal);

            foreach (var code in accountBranchCodes)
                identity.AddClaim(new Claim(BranchCodeClaims.ClaimType, code));

            return;
        }

        // (2) Kayıtta alan yok → token claim'i varsa (eski kullanıcı) olduğu gibi bırakılır.
        if (principal.HasClaim(c => c.Type == BranchCodeClaims.ClaimType))
            return;

        // (3) Personel kaydı yedeği — mevcut kullanıcılar için geçiş adımı
        var cacheKey = $"user-branch-codes:{keycloakUserId}";

        if (!_cache.TryGetValue(cacheKey, out string? joined))
        {
            var codes = await LookupBranchCodesAsync(keycloakUserId);
            joined = string.Join(',', codes);
            _cache.Set(cacheKey, joined, CacheDuration);
        }

        if (string.IsNullOrEmpty(joined))
            return;

        foreach (var code in BranchCodeClaims.Parse(joined))
            identity.AddClaim(new Claim(BranchCodeClaims.ClaimType, code));
    }

    /// <summary>
    /// Token'dan gelen <c>branch_codes</c> claim'lerini siler (#126 güvenlik düzeltmesi).
    ///
    /// <para>Kullanıcı kaydındaki değer otoriter olduğu için, kullanıcının kendi
    /// yazabileceği bir kaynaktan gelen değerlerin principal üzerinde kalmasına izin
    /// verilmez — <c>ICurrentUserService.GetBranchCodes()</c> ve <c>/auth/me</c> aynı
    /// claim'i okur.</para>
    ///
    /// <para><b>Tüm</b> identity'ler taranır, yalnız birincil olan değil:
    /// <c>ClaimsPrincipal.FindAll</c> (ki <c>BranchCodeClaims.Read</c> onu kullanır) bütün
    /// identity'lerdeki claim'leri görür. Yalnız <c>principal.Identity</c> temizlenseydi,
    /// ikinci bir identity üzerinde taşınan değer okumada hayatta kalırdı.</para>
    ///
    /// <para><c>TryRemoveClaim</c> kullanılır: sahibi olmayan identity'den kaldırma
    /// denemesinde <c>RemoveClaim</c> fırlatırdı.</para>
    /// </summary>
    private void RemoveBranchCodeClaims(ClaimsPrincipal principal)
    {
        foreach (var identity in principal.Identities)
        {
            var existing = identity.FindAll(BranchCodeClaims.ClaimType).ToList();

            foreach (var claim in existing)
            {
                if (identity.TryRemoveClaim(claim))
                    continue;

                _logger.LogWarning(
                    "Token'daki branch_codes claim'i kaldırılamadı: {ClaimValue}. " +
                    "Kapsam kararı yine de kullanıcı kaydından verilir.", claim.Value);
            }
        }
    }

    private async Task<IReadOnlyList<string>> LookupBranchCodesAsync(string keycloakUserId)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var store = scope.ServiceProvider.GetService<Marten.IDocumentStore>();
            if (store is null)
                return [];

            var conn = store.Storage.Database.CreateConnection();
            await conn.OpenAsync();
            await using (conn)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = BranchCodesLookupSql;
                cmd.Parameters.Add(new NpgsqlParameter("keycloakId", keycloakUserId));

                var codes = new List<string>();
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    if (!reader.IsDBNull(0))
                        codes.Add(reader.GetString(0));
                }

                if (codes.Count > 0)
                {
                    _logger.LogDebug(
                        "branch_codes claim eklendi (DB fallback): {KeycloakUserId} → {BranchCodes}",
                        keycloakUserId, string.Join(", ", codes));
                }

                return codes;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "branch_codes claim lookup hatası: {KeycloakUserId}", keycloakUserId);
        }

        return [];
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
        IReadOnlyList<string> DirectPermissions,
        IReadOnlyList<string> BranchCodes);
}
