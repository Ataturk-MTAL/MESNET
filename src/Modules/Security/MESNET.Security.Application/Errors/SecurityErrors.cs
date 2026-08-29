using MESNET.Common.Shared;

namespace MESNET.Security.Application.Errors;

public static class SecurityErrors
{
    public static Error UserNotFound(Guid id) =>
        new("Security.UserNotFound", $"Kullanıcı bulunamadı: {id}");

    public static Error UserAlreadyExists(string username) =>
        new("Security.UserAlreadyExists", $"Bu kullanıcı adı zaten kayıtlı: {username}");

    // Kullanıcıya ham teknik detay (HTTP kodu, exception metni) gösterilmez — detay sunucu
    // loglarındadır (catch bloklarındaki _logger.LogError). 'detail' yalnızca çağrı uyumluluğu için.
    public static Error KeycloakOperationFailed(string detail) =>
        new("Security.KeycloakOperationFailed",
            "Kimlik/yetkilendirme sunucusu işlemi şu anda gerçekleştirilemedi. Lütfen daha sonra tekrar deneyin.");

    public static Error InvalidRole(string role) =>
        new("Security.InvalidRole", $"Geçersiz rol: {role}");

    /// <summary>
    /// İstenen rol adı/adları Keycloak realm'inde bulunamadı (#129).
    ///
    /// <para>Bu hata <b>sessiz veri bozulmasının</b> yerine geçer: eskiden çözülemeyen roller
    /// filtrelenir ve işlem başarı dönerdi; kullanıcı sıfır realm rolüyle açılır, hiçbir izin
    /// almaz ve hata da görmezdi. Rol adı geçerli görünüp realm'de yoksa realm tanımı
    /// (<c>mesnet-realm.json</c>) ile <c>MesnetRoles</c> arasında sapma vardır.</para>
    /// </summary>
    public static Error RealmRolesUnresolved(IEnumerable<string> roles) =>
        new("Security.RealmRolesUnresolved",
            $"Şu rol(ler) kimlik sunucusunda tanımlı değil: {string.Join(", ", roles)}. " +
            "İşlem yapılmadı — kullanıcı yetkisiz kalmasın diye yarım uygulanmaz.");

    public static Error PermissionNotAssignableToRole(string roles, string permissions) =>
        new("Security.PermissionNotAssignableToRole",
            $"Bu yetkiler kullanıcının rolüne ({roles}) atanamaz: {permissions}. " +
            "Yetki, rolün kapsamı dışında olamaz (ör. işletme kullanıcısına kurum-yönetimi yetkisi verilemez).");

    /// <summary>
    /// Kapsam muafiyeti izni bireysel olarak atanmak istendi (#126).
    /// Bu izinler yapılandırmadan bağımsız olarak reddedilir; yalnız rol üzerinden gelebilirler.
    /// </summary>
    public static Error PermissionNeverDirectlyAssignable(string permissions) =>
        new("Security.PermissionNeverDirectlyAssignable",
            $"Bu yetkiler hiçbir kullanıcıya bireysel olarak atanamaz: {permissions}. " +
            "Veri kapsamını genişleten muafiyet izinleridir ve yalnız rol üzerinden verilebilir.");

    /// <summary>
    /// Kurum (kiracı) bağı, aktörün kendi kurum kapsamı dışına yazılmak istendi
    /// (ADR-0003 adım 2). Karar <c>UserInstitutionScopePolicy</c> içindedir.
    /// </summary>
    public static Error InstitutionScopeNotAllowed() =>
        new("Security.InstitutionScopeNotAllowed",
            "Kullanıcının kurum bağı yalnız kendi kurumunuza yazılabilir. " +
            "Başka bir kuruma bağlı kullanıcının bağı, o kurum tarafından çözülmelidir.");

    public static Error CannotDeleteSelf() =>
        new("Security.CannotDeleteSelf", "Kendi hesabınızı silemezsiniz.");

    public static Error InvitationNotFound(Guid id) =>
        new("Security.InvitationNotFound", $"Davet bulunamadı: {id}");

    public static Error InvitationAlreadyExists(string email, string role) =>
        new("Security.InvitationAlreadyExists", $"Bu email ve rol için aktif davet zaten mevcut: {email} ({role})");

    public static Error InvitationExpired(Guid id) =>
        new("Security.InvitationExpired", $"Davetin süresi dolmuş: {id}");

    public static Error InvitationNotApproved(Guid id) =>
        new("Security.InvitationNotApproved", $"Davet henüz onaylanmamış: {id}");

    public static Error InvitationAlreadyCompleted(Guid id) =>
        new("Security.InvitationAlreadyCompleted", $"Davet zaten tamamlanmış: {id}");

    public static Error InvalidInvitationStatus(Guid id, string currentStatus, string expectedStatus) =>
        new("Security.InvalidInvitationStatus",
            $"Davet durumu geçersiz: {id}. Mevcut: {currentStatus}, Beklenen: {expectedStatus}");

    /// <summary>
    /// Veli–öğrenci bağı kiracı sınırını aşamaz (#271). Mesaj <c>resync-projections</c>'a
    /// yönlendirir: en olası sebep öğrenci görünümünün hiç doldurulmamış olmasıdır ve o hâlde
    /// hata "yetkisiz" gibi görünüp operatörü yanlış yere bakmaya iter.
    /// </summary>
    public static Error GuardianLinkOutOfScope(IReadOnlyList<Guid> studentIds) =>
        new("Security.GuardianLinkOutOfScope",
            $"Şu öğrenciler bu kuruma ait değil ya da öğrenci görünümü henüz doldurulmamış: "
            + $"{string.Join(", ", studentIds)}. Görünüm boşsa "
            + "POST /api/students/resync-projections çalıştırılmalıdır.");

    /// <summary>
    /// Hedef kurum aktörün alt ağacında değil. <b>"Bulunamadı" DENMEZ</b> — kapsamı olmayan
    /// bir aktöre hangi kimliklerin var olduğunu doğrulatmak, kurum listesini tahminle
    /// taramanın kapısını açar. Aynı gerekçe <c>InstitutionErrors.InstitutionScopeDenied</c>
    /// yorumunda.
    /// </summary>
    public static Error ActiveContextOutOfScope(Guid institutionId) =>
        new("Security.ActiveContextOutOfScope",
            $"Bu kurum yetki alanınızda değil: {institutionId}");
}