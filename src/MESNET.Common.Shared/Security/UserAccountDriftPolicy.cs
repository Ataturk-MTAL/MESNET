namespace MESNET.Common.Shared.Security;

/// <summary>
/// <c>UserAccount</c> kaydının Keycloak'tan sapıp sapmadığına karar verir (#208).
///
/// <para><b>Neden gerekli:</b> izinler kayıttan türetilir ve <b>kayıt varsa token'daki rollere
/// hiç bakılmaz</b> — kayıt otoriterdir (<c>BranchCodes</c>, <c>LinkedStudentIds</c> ile aynı
/// ilke). Bilinçli bir karar, ama bedeli şu: Keycloak'ta yapılan rol değişikliği
/// <c>POST /api/security/users/sync</c> çağrılmadan sisteme <b>hiç ulaşmaz</b> ve bunu gören
/// hiçbir kontrol yoktu.</para>
///
/// <para>Yaşanan (#205): <c>admin</c>'e Keycloak'ta <c>SystemAdmin</c> atandı, token da onu
/// taşıyordu, kayıt hâlâ <c>["InstitutionManager"]</c> diyordu ve uç 403 dönmeye devam etti.
/// Hiçbir uyarı yoktu; neden çalışmadığı ancak veritabanı elle okunarak anlaşıldı.</para>
///
/// <para><b>Bu sınıf saftır</b> — HTTP, veritabanı, saat bilmez. Karar burada, okuma
/// <c>PermissionClaimsTransformation</c>'da: karar birim testinde kilitlenebilsin diye
/// (<c>RealmInvariants</c> ile aynı ayrım).</para>
///
/// <para><b>Neden açılışta değil:</b> sapma kullanıcı başına doğar ve gerçek bir kurulumda
/// binlerce kullanıcı olabilir; açılışta hepsini gezmek kabul edilemez. Kontrol, kullanıcı
/// zaten sisteme geldiğinde ve elde her iki kaynak da hazırken çalışır — maliyeti iki liste
/// karşılaştırması kadardır.</para>
/// </summary>
public static class UserAccountDriftPolicy
{
    /// <summary>
    /// Keycloak'ın her token'a koyduğu, <c>UserAccount</c>'a hiç yazılmayan teknik roller.
    ///
    /// <para>Süzülmeselerdi kontrol <b>her kullanıcıda</b> sürekli alarm çalardı — yani hiç
    /// çalışmazdı. <c>default-roles-*</c> realm adına göre değiştiği için önek olarak elenir.</para>
    /// </summary>
    private static readonly HashSet<string> TeknikRoller = new(StringComparer.OrdinalIgnoreCase)
    {
        "offline_access", "uma_authorization", "manage-account", "manage-account-links",
        "view-profile", "manage-realm", "manage-users", "view-users", "query-users"
    };

    private const string VarsayilanRolOneki = "default-roles-";

    /// <summary>
    /// Kayıtta eksik olan rolleri döndürür. Boş liste = sapma yok.
    /// </summary>
    /// <param name="recordRoles"><c>UserAccount.Roles</c> — otoriter kaynak.</param>
    /// <param name="recordWrittenAt">Kaydın son yazılma anı (<c>UpdatedAt ?? CreatedAt</c>).</param>
    /// <param name="tokenRoles">Token'daki <c>realm_access.roles</c>.</param>
    /// <param name="tokenIssuedAt">Token'ın üretilme anı (<c>iat</c>).</param>
    /// <remarks>
    /// <para><b>Zaman koşulu neden var:</b> yalnız "token'da var, kayıtta yok" bakılsaydı,
    /// uygulamadan yapılan her <b>rol kaldırma</b> işlemi token ömrü boyunca (dev realm'inde
    /// 1800 sn) yanlış alarm üretirdi — kayıt güncel, token eski. Token kayıttan <b>sonra</b>
    /// üretilmişse Keycloak kayıttan sonra değişmiş demektir; asıl aradığımız budur.</para>
    ///
    /// <para><b>Tek yön denetlenir.</b> Kayıtta fazladan rol bulunması sapma değildir: kayıt
    /// otoriter olduğu için oradaki rol zaten yürürlüktedir.</para>
    ///
    /// <para><b>Tanınmayan rol adı sapma sayılmaz.</b> Kontrol yalnız <see cref="MesnetRoles"/>
    /// listesindeki roller hakkında konuşur; realm'e elle eklenmiş bir ad için yöneticiyi
    /// uyarmak yanlış alarmdır.</para>
    /// </remarks>
    public static IReadOnlyList<string> Detect(
        IEnumerable<string> recordRoles,
        DateTime recordWrittenAt,
        IEnumerable<string> tokenRoles,
        DateTime tokenIssuedAt)
    {
        // Kaydın yazılma anı bilinmiyorsa karar verilemez. Bu dal olmasaydı DateTime.MinValue
        // her token'ı "kayıttan sonra" gösterir ve kontrol TÜM kullanıcılarda alarm çalardı.
        if (recordWrittenAt == default) return [];

        // Kayıt token kadar yeni ya da daha yeniyse divergence beklenen durumdur.
        if (ToUtc(tokenIssuedAt) <= ToUtc(recordWrittenAt)) return [];

        var kayittakiler = new HashSet<string>(recordRoles, StringComparer.OrdinalIgnoreCase);

        return [.. tokenRoles
            .Where(IlgiliRol)
            .Where(r => !kayittakiler.Contains(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(r => r, StringComparer.Ordinal)];
    }

    /// <summary>
    /// İki zaman damgasını karşılaştırmadan önce UTC'ye çeker.
    ///
    /// <para><b>Neden şart:</b> kayıt zamanı JSON'dan gelir ve ofsetli yazılmışsa
    /// (<c>...+00:00</c>) System.Text.Json onu <b>yerel saate</b> çevirir; token zamanı ise
    /// <c>iat</c>'ten her zaman UTC üretilir. Ham karşılaştırma, saat dilimi farkı kadar bir
    /// kayma yaratır ve pozitif ofsetli bölgelerde kayıt sürekli "daha yeni" görünür —
    /// <b>kontrol hiç ateşlemez</b>. Canlıda tam olarak bu yaşandı: kayıt 14:51Z verisi 17:51
    /// yerel olarak okundu, 15:57Z token'ı ondan eski sayıldı.</para>
    ///
    /// <para><c>Unspecified</c> UTC kabul edilir — kayıtlar <c>DateTime.UtcNow</c> ile yazılır.</para>
    /// </summary>
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static bool IlgiliRol(string rol) =>
        !string.IsNullOrWhiteSpace(rol)
        && !TeknikRoller.Contains(rol)
        && !rol.StartsWith(VarsayilanRolOneki, StringComparison.OrdinalIgnoreCase)
        && MesnetRoles.All.Contains(rol, StringComparer.OrdinalIgnoreCase);

    /// <summary>Log için tek biçimli, eyleme dönüşebilir özet.</summary>
    public static string Describe(string username, IReadOnlyList<string> eksikRoller) =>
        $"UserAccount kaydı Keycloak'tan sapmış — kullanıcı '{username}', kayıtta eksik rol: "
        + $"{string.Join(", ", eksikRoller)}. Kayıt otoriter olduğu için bu roller izin ÜRETMEZ "
        + $"ve ilgili uçlar 403 döner. Düzeltme: POST /api/security/users/sync (izin önbelleği "
        + $"nedeniyle etkisi 5 dakikaya kadar gecikebilir).";
}
