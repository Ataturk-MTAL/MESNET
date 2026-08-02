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

        var cacheKey = $"user-permissions:{sub}";

        if (!_cache.TryGetValue(cacheKey, out PermissionCacheEntry? entry))
        {
            entry = await LoadFromMartenAsync(sub);

            if (entry is not null)
            {
                _cache.Set(cacheKey, entry, CacheDuration);
            }
        }

        // ── Kurum kapsamı claim enrichment ──
        // Kullanıcı kaydı ÖNCE yüklenir: kayıt otoriterdir ve token claim'ini ezer.
        // (Eskiden bu çağrı entry yüklenmeden önce yapılıyor ve token'daki claim varsa
        // kayda hiç bakılmıyordu.)
        await EnrichInstitutionClaimAsync(principal, sub, entry?.InstitutionId);

        // ── Alan (branş) kapsamı claim enrichment (#126) ──
        // Sıra: token claim → kullanıcı kaydındaki BranchCodes → personel kaydı yedeği.
        // Kayıt sırasında girilen bilgi birincil kaynaktır; personel kaydı yalnız mevcut
        // kullanıcılar için geçiş adımıdır. Hiçbiri yoksa claim eklenmez — bu geçerli bir
        // durumdur (müdür/müdür yardımcısı hiçbir alana bağlı değildir).
        await EnrichBranchCodesClaimAsync(principal, sub, entry?.BranchCodes);

        // ── Veli–öğrenci bağ kapsamı claim enrichment (#174) ──
        // BranchCodes ile aynı güven sırası: KAYIT otoriterdir. Kayıt doluysa token'dan gelen
        // linked_student_ids claim'leri silinir. DB yedeği YOKTUR — bağ yalnız kullanıcı
        // kaydında tutulur; kayıt yoksa kapsam yoktur ve erişim doğmaz.
        EnrichLinkedStudentClaims(principal, entry?.LinkedStudentIds);

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
                info.IsEnabled, info.Roles, info.DirectPermissions, info.BranchCodes,
                info.InstitutionId, info.LinkedStudentIds ?? []);
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
    /// <c>linked_student_ids</c> claim'ini kullanıcı kaydından kurar (#174).
    ///
    /// <para><b>Kayıt otoriterdir.</b> Token'dan gelen değerler HER ZAMAN silinir — kayıt boş
    /// olsa bile. <c>branch_codes</c>'ta token yedeği bırakılmıştı (mevcut kullanıcılar için
    /// geçiş adımı); burada yedek YOKTUR çünkü bağ kaydı bu iş ile birlikte doğdu ve token'dan
    /// gelen bir değerin meşru kaynağı olamaz. Öznitelik Keycloak'ta <i>unmanaged</i>'dır:
    /// yedek bırakılsaydı kullanıcı kendi Account konsolundan kendine öğrenci ekleyip başka
    /// bir öğrencinin verisine erişebilirdi.</para>
    /// </summary>
    private void EnrichLinkedStudentClaims(
        ClaimsPrincipal principal, IReadOnlyList<Guid>? accountLinkedStudentIds)
    {
        if (principal.Identity is not ClaimsIdentity identity)
            return;

        RemoveLinkedStudentClaims(principal);

        if (accountLinkedStudentIds is not { Count: > 0 })
            return;

        foreach (var studentId in accountLinkedStudentIds)
            identity.AddClaim(new Claim(LinkedStudentClaims.ClaimType, studentId.ToString()));
    }

    /// <summary>
    /// Token'daki <c>linked_student_ids</c> claim'lerini siler. Gerekçe
    /// <see cref="RemoveBranchCodeClaims"/> ile aynı; silinemeyen claim sessizce bırakılmaz,
    /// loglanır.
    /// </summary>
    private void RemoveLinkedStudentClaims(ClaimsPrincipal principal)
    {
        foreach (var identity in principal.Identities)
        {
            var existing = identity.FindAll(LinkedStudentClaims.ClaimType).ToList();
            foreach (var claim in existing)
            {
                try
                {
                    identity.RemoveClaim(claim);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex,
                        "Token'daki linked_student_ids claim'i kaldırılamadı: {ClaimValue}. " +
                        "Kullanıcı tarafından belirlenmiş kapsam geçerli kalabilir.", claim.Value);
                }
            }
        }
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
    /// <c>institution_id</c> claim'ini çözer. Öncelik sırası — <c>branch_codes</c> (#126) ile
    /// <b>birebir aynıdır</b>:
    ///
    /// <list type="number">
    ///   <item><b>Kullanıcı kaydı</b> (<c>UserAccount.InstitutionId</c>) — <b>OTORİTERDİR</b>.
    ///         Doluysa token'dan gelen <c>institution_id</c> claim'i <b>atılır</b> ve yerine
    ///         kayıttaki değer konur</item>
    ///   <item><b>Token claim'i</b> — yalnız kullanıcı kaydında kurum YOKKEN kabul edilir</item>
    ///   <item><b>Personel kaydı yedeği</b> — kurum belgesindeki <c>staff[]</c> eşleşmesi;
    ///         geçiş adımıdır, birincil yol DEĞİLDİR</item>
    /// </list>
    ///
    /// <para><b>GÜVENLİK — bu sırayı TERS ÇEVİRMEYİN.</b> Önceden tam tersi yapılıyordu:
    /// token'da claim varsa kayda <i>hiç bakılmıyordu</i>. <c>institution_id</c>, tıpkı
    /// <c>branch_codes</c> gibi, Keycloak'ta <i>unmanaged</i> bir kullanıcı özniteliğidir;
    /// realm politikası yanlışlıkla <c>ENABLED</c>'a çekilirse (ya da başka bir realm/ortam
    /// öyle kurulursa) kullanıcı varsayılan <c>manage-account</c> rolüyle kendi Account
    /// konsolundan <b>kendi kurumunu yazabilirdi</b>. Token imzalı olması içeriğin
    /// <b>kullanıcı tarafından belirlenmediği</b> anlamına gelmez.</para>
    ///
    /// <para>Bu, çok kurumlu (Faz 2) yapıya geçilirken kritik hâle gelir: <c>institution_id</c>
    /// kiracı anahtarı adayıdır. Kiracı sınırının, kullanıcının yazabileceği bir kaynaktan
    /// gelmesi izolasyonun tamamını geçersiz kılardı. Bugün sistemde tek kurum bulunduğu için
    /// istismar senaryosu oluşmuyor; düzeltme o güne bırakılmadı.</para>
    ///
    /// <para>Realm tarafında politika <c>ADMIN_EDIT</c>'tir; buradaki kontrol o yapılandırmaya
    /// <b>bağımlı olmayan</b> ikinci katmandır. İkisinden biri kaldırılırsa açık geri gelir.</para>
    /// </summary>
    private async Task EnrichInstitutionClaimAsync(
        ClaimsPrincipal principal, string keycloakUserId, Guid? accountInstitutionId)
    {
        if (principal.Identity is not ClaimsIdentity identity)
            return;

        // (1) Kullanıcı kaydı OTORİTERDİR — token'dan geleni ezer.
        if (accountInstitutionId is { } institution && institution != Guid.Empty)
        {
            RemoveInstitutionClaims(principal);
            identity.AddClaim(new Claim("institution_id", institution.ToString()));
            return;
        }

        // (2) Kayıtta kurum yok → token claim'i varsa olduğu gibi bırakılır.
        if (principal.HasClaim(c => c.Type == "institution_id"))
            return;

        // (3) Personel kaydı yedeği — mevcut kullanıcılar için geçiş adımı.
        var cacheKey = $"user-institution:{keycloakUserId}";

        if (!_cache.TryGetValue(cacheKey, out string? institutionId))
        {
            institutionId = await LookupInstitutionIdAsync(keycloakUserId);
            _cache.Set(cacheKey, institutionId ?? string.Empty, CacheDuration);
        }

        if (!string.IsNullOrEmpty(institutionId))
        {
            identity.AddClaim(new Claim("institution_id", institutionId));
        }
    }

    /// <summary>
    /// Token'dan gelen <c>institution_id</c> claim'lerini siler.
    ///
    /// <para><b>Tüm</b> identity'ler taranır, yalnız birincil olan değil: okuma tarafı
    /// <c>ClaimsPrincipal.FindFirst</c> kullanır ve bütün identity'lerdeki claim'leri görür.
    /// Yalnız <c>principal.Identity</c> temizlenseydi, ikinci bir identity üzerinde taşınan
    /// değer okumada hayatta kalırdı — <c>RemoveBranchCodeClaims</c> ile aynı gerekçe.</para>
    /// </summary>
    private void RemoveInstitutionClaims(ClaimsPrincipal principal)
    {
        foreach (var identity in principal.Identities)
        {
            var existing = identity.FindAll("institution_id").ToList();

            foreach (var claim in existing)
            {
                if (identity.TryRemoveClaim(claim))
                    continue;

                _logger.LogWarning(
                    "Token'daki institution_id claim'i kaldırılamadı: {ClaimValue}. " +
                    "Kapsam kararı yine de kullanıcı kaydından verilir.", claim.Value);
            }
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
        IReadOnlyList<string> BranchCodes,
        Guid? InstitutionId,
        IReadOnlyList<Guid> LinkedStudentIds);
}
