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
}
