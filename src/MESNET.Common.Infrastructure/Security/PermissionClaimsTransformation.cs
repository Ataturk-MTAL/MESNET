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
/// Ayrıca kapsam claim'lerini (institution_id, branch_codes, linked_student_ids) sunucu
/// tarafındaki kayıttan çözer. institution_id için token'dan gelen değer HİÇ kabul edilmez
/// (ADR-0003 adım 2) — kiracı anahtarı kullanıcının yazabileceği bir kaynaktan gelemez.
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

                // Sapma kontrolü BİLEREK cache miss dalında (#208): girdi 5 dakika yaşadığı
                // için uyarı kullanıcı başına en fazla o sıklıkta çıkar. Her istekte
                // çalıştırılsaydı tek bir sapma logu boğardı ve kontrol görmezden gelinirdi.
                WarnIfAccountDrifted(principal, entry);
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
                info.InstitutionId, info.LinkedStudentIds ?? [], info.RolesWrittenAt);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UserAccount yüklenirken hata: {KeycloakUserId}", keycloakUserId);
            return null;
        }
    }

    /// <summary>
    /// <c>UserAccount</c> kaydı Keycloak'tan sapmışsa uyarır (#208).
    ///
    /// <para>Kayıt otoriter olduğu için token'daki doğru rol <b>hiç kullanılmaz</b>; kullanıcı
    /// yalnız 403 görür ve nedenini anlamaz. Kararın kendisi saf
    /// <see cref="UserAccountDriftPolicy"/> içinde ve testle kilitli; burada yalnız okuma var.</para>
    ///
    /// <para>Kontrol <b>erişimi değiştirmez</b> — yalnız log üretir. Sapmayı otomatik düzeltmek
    /// (token'daki rolü kullanmak) kaydın otoriterliğini sessizce iptal ederdi; o karar
    /// yöneticinindir.</para>
    /// </summary>
    private void WarnIfAccountDrifted(ClaimsPrincipal principal, PermissionCacheEntry entry)
    {
        if (ReadIssuedAt(principal) is not { } issuedAt) return;

        var missing = UserAccountDriftPolicy.Detect(
            entry.Roles, entry.RolesWrittenAt, ExtractRealmRolesFromToken(principal), issuedAt);

        if (missing.Count == 0) return;

        var username = principal.FindFirst("preferred_username")?.Value ?? "(bilinmiyor)";
        _logger.LogWarning("{Rapor}", UserAccountDriftPolicy.Describe(username, missing));
    }

    /// <summary>
    /// Token'ın üretilme anı (<c>iat</c>, Unix saniye). Okunamazsa <c>null</c> — sapma
    /// <b>iddia edilmez</b>, çünkü zaman koşulu olmadan yanlış alarm kaçınılmazdır.
    /// </summary>
    private static DateTime? ReadIssuedAt(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirst("iat")?.Value;

        return long.TryParse(raw, out var unixSeconds)
            ? DateTimeOffset.FromUnixTimeSeconds(unixSeconds).UtcDateTime
            : null;
    }

    /// <summary>
    /// Cache'den kullanıcı permission bilgisini invalidate eder.
    /// ChangeUserRolesHandler ve ChangeUserPermissionsHandler tarafından çağrılır.
    /// </summary>
    public static void InvalidateCache(IMemoryCache cache, string keycloakUserId)
    {
        cache.Remove($"user-permissions:{keycloakUserId}");
        cache.Remove($"user-institution:{keycloakUserId}");
        cache.Remove($"user-institution-warned:{keycloakUserId}");
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
    /// <c>institution_id</c> claim'ini çözer. <b>Token'dan gelen değer HİÇBİR koşulda kabul
    /// edilmez</b> (ADR-0003 adım 2); yalnız sunucu tarafı iki kaynak vardır:
    ///
    /// <list type="number">
    ///   <item><b>Kullanıcı kaydı</b> (<c>UserAccount.InstitutionId</c>) — <b>OTORİTERDİR</b></item>
    ///   <item><b>Personel kaydı yedeği</b> — kurum belgesindeki <c>staff[]</c> eşleşmesi;
    ///         geçiş adımıdır, birincil yol DEĞİLDİR</item>
    /// </list>
    ///
    /// <para>İkisi de boşsa claim <b>hiç eklenmez</b> ve kullanıcı kurum kapsamsız kalır.
    /// Bu sessiz bir başarısızlık değildir: kapsam isteyen uçlar açık hata döner ve durum
    /// <c>LogInformation</c> ile kaydedilir.</para>
    ///
    /// <para><b>GÜVENLİK — token kabul yolunu geri eklemeyin.</b> <c>institution_id</c>
    /// Keycloak'ta <i>unmanaged</i> bir kullanıcı özniteliğidir; realm politikası yanlışlıkla
    /// <c>ENABLED</c>'a çekilirse (ya da başka bir realm/ortam öyle kurulursa) kullanıcı
    /// varsayılan <c>manage-account</c> rolüyle kendi Account konsolundan <b>kendi kurumunu
    /// yazabilirdi</b>. Token imzalı olması içeriğin <b>kullanıcı tarafından belirlenmediği</b>
    /// anlamına gelmez. Realm tarafındaki <c>ADMIN_EDIT</c> politikası ayrı bir katmandır;
    /// buradaki kontrol ona <b>bağımlı değildir</b>.</para>
    ///
    /// <para><b>Neden <c>branch_codes</c>'tan daha katı:</b> alan kapsamı kiracı <i>içinde</i>
    /// bir yetki sınırıdır; <c>institution_id</c> ise <b>kiracı anahtarının kendisidir</b>
    /// (ADR-0003). Kiracı sınırının kullanıcının yazabileceği bir kaynaktan gelmesi, satır
    /// bazlı izolasyonun tamamını geçersiz kılar — orada "yedek kaynak" diye bir şey olamaz.</para>
    ///
    /// <para><b>Ön koşul:</b> mevcut kullanıcıların kaydı doldurulmadan bu yol kapatılamaz —
    /// bkz. <c>StaffAccountBackfillPolicy</c> (adım 2.1) ve
    /// <c>POST /api/institutions/staff/resync-branch-codes</c>.</para>
    /// </summary>
    private async Task EnrichInstitutionClaimAsync(
        ClaimsPrincipal principal, string keycloakUserId, Guid? accountInstitutionId)
    {
        if (principal.Identity is not ClaimsIdentity identity)
            return;

        // Token'daki değer HER DURUMDA atılır — sunucu tarafı kaynak bulunamasa bile.
        // Aksi hâlde "kaynak yoksa token'a düş" davranışı geri gelirdi.
        RemoveInstitutionClaims(principal);

        // (1) Kullanıcı kaydı — kiracı anahtarının otoritesi.
        if (accountInstitutionId is { } institution && institution != Guid.Empty)
        {
            identity.AddClaim(new Claim("institution_id", institution.ToString()));
            return;
        }

        // (2) Personel kaydı yedeği — mevcut kullanıcılar için geçiş adımı.
        var cacheKey = $"user-institution:{keycloakUserId}";

        if (!_cache.TryGetValue(cacheKey, out string? institutionId))
        {
            institutionId = await LookupInstitutionIdAsync(keycloakUserId);

            // ── SONUÇSUZ ARAMA ÖNBELLEĞE ALINMAZ ──
            //
            // Eskiden boş sonuç da CacheDuration boyunca saklanıyordu ve bu, geçici bir
            // durumu 5 dakikalık bir kesintiye çeviriyordu: kullanıcı personel kaydına
            // eklendikten SONRA bile kapsamsız kalıyordu, çünkü önbellekte "kurumu yok"
            // yazıyordu. Kaydı olmayan kullanıcıda (henüz senkronize edilmemiş) tüketici
            // tarafı da devreye giremez — `StaffBranchSyncConsumer` hesap bulamayınca erken
            // döner, yani önbelleği temizleyecek kimse yoktur.
            //
            // Boş veritabanında tam olarak bu yaşandı: kurum oluşturulurken önbelleğe "yok"
            // yazıldı, saniyeler sonra personel eklendi ve kurum kapsamı isteyen işletme
            // kaydı 422 döndü. Token yolu bunu maskeliyordu (#204/#208 ile aynı sınıf hata).
            //
            // Bedel: kapsamsız kullanıcı için istek başına bir sorgu. Kapsamsızlık zaten
            // düzeltilmesi gereken bir anomalidir, sürekli bir durum değil; o kullanıcının
            // istekleri de çoğunlukla reddedilecektir.
            if (!string.IsNullOrEmpty(institutionId))
                _cache.Set(cacheKey, institutionId, CacheDuration);
            else
                WarnScopeless(keycloakUserId);
        }

        if (!string.IsNullOrEmpty(institutionId))
        {
            identity.AddClaim(new Claim("institution_id", institutionId));
        }
    }

    /// <summary>
    /// Kapsamsızlığı görünür kılar. Arama artık her istekte yapıldığı için uyarı <b>ayrı bir
    /// anahtarla</b> kısılır — aksi hâlde tek bir kapsamsız kullanıcı logu boğar ve uyarı
    /// görmezden gelinir hâle gelirdi (#208 ile aynı gerekçe).
    /// </summary>
    private void WarnScopeless(string keycloakUserId)
    {
        var logKey = $"user-institution-warned:{keycloakUserId}";
        if (_cache.TryGetValue(logKey, out _)) return;

        _cache.Set(logKey, true, CacheDuration);

        _logger.LogInformation(
            "Kullanıcının kurum kapsamı yok: {KeycloakUserId}. Kayıt da personel eşleşmesi de " +
            "boş — kurum kapsamı isteyen uçlar hata dönecek. Çözüm: " +
            "POST /api/security/users/{{id}}/institution", keycloakUserId);
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

    /// <param name="RolesWrittenAt">
    /// Kaydın son yazılma anı — sapma tespitinin zaman koşulu için (#208). Token bundan
    /// SONRA üretilmişse Keycloak kayıttan sonra değişmiş demektir.
    /// </param>
    private sealed record PermissionCacheEntry(
        bool IsEnabled,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> DirectPermissions,
        IReadOnlyList<string> BranchCodes,
        Guid? InstitutionId,
        IReadOnlyList<Guid> LinkedStudentIds,
        DateTime RolesWrittenAt);
}
