namespace MESNET.Common.Shared.Security;

/// <summary>
/// Çalışan Keycloak realm'inin depodaki tanımdan sapmadığını doğrular (#195).
///
/// <para><b>Neden gerekli:</b> realm import <b>tek seferliktir</b> — <c>start-dev --import-realm</c>
/// yalnız boş veritabanında çalışır ve import edilen kopya kalıcı volume'ün içinde kalır. Depodaki
/// <c>mesnet-realm.json</c>'a sonradan eklenen her ayar (rol, politika, client) <b>mevcut bir kaba
/// hiç ulaşmaz</b>. Sapma sessizdir: uygulama çalışır, testler yeşildir, yalnız güvenlik katmanı
/// yoktur.</para>
///
/// <para>Canlı örnek: <c>unmanagedAttributePolicy</c> depoda <c>ADMIN_EDIT</c>, çalışan dev
/// realm'inde <c>ENABLED</c>'dı. Mart'ta alınan kopyada <c>components</c> anahtarı hiç yoktu.
/// #126'nın ikinci savunma katmanı o ortamda hiç aktif olmamıştı.</para>
///
/// <para><b>Bu sınıf saftır</b> — HTTP bilmez, yalnız okunmuş bir <see cref="RealmSnapshot"/>'ı
/// değerlendirir. Okuma ayrı, karar ayrı: karar birim testinde kilitlenebilsin diye.</para>
/// </summary>
public static class RealmInvariants
{
    /// <summary>
    /// Yönetilmeyen (unmanaged) kullanıcı özniteliklerinde beklenen politika.
    ///
    /// <para><c>ENABLED</c> olsaydı kullanıcı <c>manage-account</c> ile kendi Account konsolundan
    /// kendine <c>branch_codes</c> ekleyip kapsamını genişletebilirdi. <c>ADMIN_EDIT</c> yazmayı
    /// yalnız yöneticiye bırakır.</para>
    ///
    /// <para>Bu <b>ikinci</b> katmandır; birincisi koddadır ve otoriterdir — <c>UserAccount</c>
    /// kaydı token claim'ini ezer. İkisi birden gitmeden kapsam aşılamaz, ama derinlemesine
    /// savunmanın sessizce kaybolması da kabul edilemez.</para>
    /// </summary>
    public const string ExpectedUnmanagedAttributePolicy = "ADMIN_EDIT";

    /// <summary>PKCE akışı public client ister — client secret taşımayan SPA.</summary>
    public const string WebClientId = "mesnet-web";

    /// <summary>
    /// Depodaki realm tanımının tohum kullanıcılarına vaat ettiği realm rolleri (#205).
    ///
    /// <para><b>Neden rol listesi yetmiyor:</b> rolün realm'de <b>var olması</b> ile kullanıcıya
    /// <b>atanmış olması</b> ayrı şeylerdir ve import ikisini ayrı yerde taşır. Canlı dev
    /// realm'inde 11 rolün tamamı vardı — <see cref="VerifyRealmRoles"/> temiz geçiyordu — ama
    /// <c>admin</c> kullanıcısında yalnız <c>InstitutionManager</c> atanmıştı. <c>SystemAdmin</c>
    /// eksik olduğu için <c>platform:parameter:manage</c> hiç gelmedi;
    /// <c>PUT /api/payments/config/minimum-wage</c> 403 döndü ve asgari ücret dev'de hiç
    /// girilemedi.</para>
    ///
    /// <para><b>Var olmayan kullanıcı sapma değildir.</b> Bunlar yalnız geliştirme realm'inin
    /// tohum verisidir; gerçek kurulumda hiçbiri bulunmaz. Denetlenen tek şey, <b>var olan</b>
    /// kullanıcının eksik rolüdür — aksi hâlde her üretim açılışı yanlış alarm çalardı.</para>
    ///
    /// <para>Kilit: <c>RealmInvariantsTests.Beklenen_kullanici_rolleri_depodaki_realm_tanimiyla_ayni</c>
    /// bu haritayı <c>mesnet-realm.json</c> ile karşılaştırır. Realm'e eklenen bir atama buraya
    /// yansımazsa denetim onu <b>hiç aramaz</b> — #205'in kaçırdığı durum tam budur.</para>
    /// </summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ExpectedSeedUserRoles =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["admin"] = [MesnetRoles.InstitutionManager, MesnetRoles.SystemAdmin],
            ["viceprincipal"] = [MesnetRoles.InstitutionManager],
            ["teacher1"] = [MesnetRoles.Teacher],
            ["teacher2"] = [MesnetRoles.Teacher],
            ["teacher3"] = [MesnetRoles.Teacher],
            ["student1"] = [MesnetRoles.Student]
        };

    /// <summary>
    /// Sapmaları döndürür. Boş liste = realm depodaki tanımla uyumlu.
    ///
    /// <para>Okunamayan alan (<c>null</c>) sapma <b>sayılmaz</b>: yetki eksikliği ya da sürüm
    /// farkı yüzünden okunamamış olabilir ve "bilmiyorum"u "bozuk"tan ayırmak gerekir. Okunamayan
    /// alanlar <see cref="RealmSnapshot.UnreadableFields"/> ile ayrıca raporlanır.</para>
    /// </summary>
    public static IReadOnlyList<RealmDrift> Verify(RealmSnapshot snapshot)
    {
        var drifts = new List<RealmDrift>();

        VerifyUnmanagedAttributePolicy(snapshot, drifts);
        VerifyRealmRoles(snapshot, drifts);
        VerifySeedUserRoles(snapshot, drifts);
        VerifyWebClientIsPublic(snapshot, drifts);

        return drifts;
    }

    private static void VerifyUnmanagedAttributePolicy(RealmSnapshot snapshot, List<RealmDrift> drifts)
    {
        if (snapshot.UnmanagedAttributePolicy is not { Length: > 0 } policy) return;
        if (string.Equals(policy, ExpectedUnmanagedAttributePolicy, StringComparison.Ordinal)) return;

        drifts.Add(new RealmDrift(
            "unmanagedAttributePolicy",
            ExpectedUnmanagedAttributePolicy,
            policy,
            $"Kullanıcı kendi Account konsolundan kendine branch_codes ekleyip kapsamını "
            + $"genişletebilir (#126'nın ikinci katmanı). Düzeltme: PUT /admin/realms/{{realm}}/users/profile "
            + $"— unmanagedAttributePolicy: \"{ExpectedUnmanagedAttributePolicy}\"."));
    }

    private static void VerifyRealmRoles(RealmSnapshot snapshot, List<RealmDrift> drifts)
    {
        if (snapshot.RealmRoles is not { Count: > 0 } roles) return;

        var existing = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase);
        var missing = MesnetRoles.All.Where(r => !existing.Contains(r)).ToList();
        if (missing.Count == 0) return;

        drifts.Add(new RealmDrift(
            "realm roles",
            $"{MesnetRoles.All.Count} rolün tamamı",
            $"eksik: {string.Join(", ", missing)}",
            "Rol atama sessizce yarım kalır; kullanıcı giriş yapar ama hiçbir şey göremez. "
            + "Düzeltme: eksik rolleri realm'e ekleyin ya da realm'i yeniden import edin."));
    }

    /// <summary>
    /// Kullanıcı→rol atamalarını denetler (#205). Her kullanıcı <b>ayrı</b> sapma satırı üretir;
    /// tek satırda toplansaydı ikinci eksik ilkinin arkasında kalırdı.
    /// </summary>
    private static void VerifySeedUserRoles(RealmSnapshot snapshot, List<RealmDrift> drifts)
    {
        if (snapshot.SeedUserRoles is not { } assignments) return;

        foreach (var (user, expected) in ExpectedSeedUserRoles)
        {
            // Kullanıcı yoksa sapma değil — üretimde tohum kullanıcıları hiç bulunmaz.
            if (!assignments.TryGetValue(user, out var mevcutRoller)) continue;

            var existing = new HashSet<string>(mevcutRoller, StringComparer.OrdinalIgnoreCase);
            var missing = expected.Where(r => !existing.Contains(r)).ToList();
            if (missing.Count == 0) continue;

            drifts.Add(new RealmDrift(
                $"kullanıcı {user} → realm rolleri",
                string.Join(", ", expected),
                mevcutRoller.Count == 0 ? "hiç rol yok" : $"eksik: {string.Join(", ", missing)}",
                $"Rol realm'de var ama atanmamış; kullanıcı o rolün izinlerini hiç almaz ve "
                + $"ilgili uçlar 403 döner. Düzeltme: POST /admin/realms/{{realm}}/users/{{id}}"
                + $"/role-mappings/realm — eksik roller: {string.Join(", ", missing)}."));
        }
    }

    private static void VerifyWebClientIsPublic(RealmSnapshot snapshot, List<RealmDrift> drifts)
    {
        if (snapshot.WebClientIsPublic is not { } isPublic || isPublic) return;

        drifts.Add(new RealmDrift(
            $"client {WebClientId}.publicClient",
            "true",
            "false",
            "SPA'da client secret tutulamaz; confidential client PKCE akışını kırar. "
            + $"Düzeltme: {WebClientId} client'ını public yapın."));
    }

    /// <summary>Log ve hata mesajı için tek biçimli özet üretir.</summary>
    public static string Describe(IReadOnlyList<RealmDrift> drifts) =>
        string.Join(
            Environment.NewLine,
            drifts.Select(d =>
                $"  • {d.Key}: beklenen '{d.Expected}', bulunan '{d.Actual}'{Environment.NewLine}"
                + $"    {d.Impact}"));
}

/// <summary>
/// Çalışan realm'den okunan ayarların anlık görüntüsü. Okunamayan alan <c>null</c> bırakılır —
/// "bilmiyorum" ile "bozuk" ayrı şeylerdir.
/// </summary>
/// <param name="UnmanagedAttributePolicy">
/// <c>GET /admin/realms/{realm}/users/profile</c> → <c>unmanagedAttributePolicy</c>.
/// </param>
/// <param name="RealmRoles"><c>GET /admin/realms/{realm}/roles</c> → rol adları.</param>
/// <param name="WebClientIsPublic">
/// <c>GET /admin/realms/{realm}/clients?clientId=mesnet-web</c> → <c>publicClient</c>.
/// </param>
/// <param name="UnreadableFields">Okunamayan alanların adları — sapma değil, eksik bilgi.</param>
/// <param name="SeedUserRoles">
/// <c>GET /admin/realms/{realm}/users/{id}/role-mappings/realm</c> → kullanıcı adı başına rol
/// adları. <b>Yalnız bulunan</b> kullanıcılar yer alır; anahtarın yokluğu "kullanıcı bu realm'de
/// yok" demektir ve sapma sayılmaz. Sözlüğün tamamen <c>null</c> olması "hiç okunamadı" demektir.
/// </param>
public sealed record RealmSnapshot(
    string? UnmanagedAttributePolicy = null,
    IReadOnlyList<string>? RealmRoles = null,
    bool? WebClientIsPublic = null,
    IReadOnlyList<string>? UnreadableFields = null,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? SeedUserRoles = null);

/// <summary>Tek bir sapma: ne bekleniyordu, ne bulundu, neye yol açar.</summary>
/// <param name="Key">Sapan ayarın adı.</param>
/// <param name="Expected">Depodaki tanımın beklediği değer.</param>
/// <param name="Actual">Çalışan realm'de bulunan değer.</param>
/// <param name="Impact">Sonucu ve düzeltme yolu — mesaj eyleme dönüşebilsin diye.</param>
public sealed record RealmDrift(string Key, string Expected, string Actual, string Impact);
