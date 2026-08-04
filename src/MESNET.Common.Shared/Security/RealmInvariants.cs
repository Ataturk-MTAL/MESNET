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

        var mevcut = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase);
        var eksik = MesnetRoles.All.Where(r => !mevcut.Contains(r)).ToList();
        if (eksik.Count == 0) return;

        drifts.Add(new RealmDrift(
            "realm roles",
            $"{MesnetRoles.All.Count} rolün tamamı",
            $"eksik: {string.Join(", ", eksik)}",
            "Rol atama sessizce yarım kalır; kullanıcı giriş yapar ama hiçbir şey göremez. "
            + "Düzeltme: eksik rolleri realm'e ekleyin ya da realm'i yeniden import edin."));
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
public sealed record RealmSnapshot(
    string? UnmanagedAttributePolicy = null,
    IReadOnlyList<string>? RealmRoles = null,
    bool? WebClientIsPublic = null,
    IReadOnlyList<string>? UnreadableFields = null);

/// <summary>Tek bir sapma: ne bekleniyordu, ne bulundu, neye yol açar.</summary>
/// <param name="Key">Sapan ayarın adı.</param>
/// <param name="Expected">Depodaki tanımın beklediği değer.</param>
/// <param name="Actual">Çalışan realm'de bulunan değer.</param>
/// <param name="Impact">Sonucu ve düzeltme yolu — mesaj eyleme dönüşebilsin diye.</param>
public sealed record RealmDrift(string Key, string Expected, string Actual, string Impact);
