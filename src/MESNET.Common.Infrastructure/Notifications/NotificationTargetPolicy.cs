namespace MESNET.Common.Infrastructure.Notifications;

/// <summary>
/// Bir bildirim, bağlı kullanıcıya <b>ulaşır mı</b> — saf karar (#247).
///
/// <para><b>Neden ayrı bir politika:</b> karar önce <c>SseConnectionManager</c> içinde private
/// bir metottu ve <b>tek bir testi bile yoktu</b>. Hedefleme sessiz bir yüzeydir: hedef kitle
/// boş çıkarsa servis yalnız <c>LogDebug</c> yazar (<see cref="SseNotificationService"/>), yani
/// yanlış hedefleme ne hata verir ne iz bırakır. Karar saf bir fonksiyona çıkınca
/// kilitlenebilir hâle gelir — deponun <c>BranchScopePolicy</c> / <c>PlacementScopePolicy</c>
/// deseni.</para>
///
/// <para><b>Ölçütler OR mantığıyla çalışır</b> — herhangi birine uyan kullanıcı bildirimi alır.
/// Bu, <b>tanımlayıcı</b> ölçütlerde (kullanıcı, öğrenci, veli, işletme) güvenlidir: hepsi
/// evrensel benzersiz kimliklerdir ve kiracı sınırını kendiliğinden korurlar.</para>
///
/// <para><b>UYARI — <c>Roles</c> ve <c>RequiredPermission</c> ölçütleri kiracı sınırını
/// KORUMAZ.</b> Bunlar <c>InstitutionId</c> ile aynı hedefte kullanıldığında OR mantığı kurum
/// süzgecini etkisiz kılar ve bildirim başka okullara sızar. Ölçülmüş, ayrı iş: <b>#266</b>.
/// Yeni hedefleme yazarken tanımlayıcı ölçütleri tercih edin.</para>
/// </summary>
public static class NotificationTargetPolicy
{
    public static bool Matches(SseUserContext user, NotificationTarget target)
    {
        // Doğrudan userId eşleşme
        if (target.UserIds is { Count: > 0 } && target.UserIds.Contains(user.UserId))
            return true;

        // Öğrenci profili eşleşme — modüller öğrencinin Keycloak ID'sini bilmeden hedefleyebilsin (#69).
        // Yalnız öğrencinin KENDİSİNE ulaşır; velisi için GuardianOfStudentIds kullanılır.
        if (target.StudentIds is { Count: > 0 } && user.StudentId.HasValue
            && target.StudentIds.Contains(user.StudentId.Value))
            return true;

        // Veli eşleşme (#174, #247) — kullanıcının bağlı olduğu öğrencilerden herhangi biri.
        //
        // Ayrı bir boyut olmasının nedeni: veli ile öğrenci AYRI hedeflenebilmeli. Md. 36 (4)
        // veliye KOŞULSUZ, öğrenciye yalnız 18 yaşını doldurmuşsa bildirim istiyor; tek boyut
        // olsaydı 18 altı öğrenci velisinin tebligatını da görürdü.
        if (target.GuardianOfStudentIds is { Count: > 0 }
            && user.LinkedStudents.Any(target.GuardianOfStudentIds.Contains))
            return true;

        // İşletme eşleşme (#247). business_id claim'i SUNUCU tarafından üretilir — token'daki
        // değer her istekte silinip UserAccount.BusinessId'den yeniden yazılır (#229), yani
        // kullanıcı kendi hedefini belirleyemez.
        if (target.BusinessIds is { Count: > 0 } && user.BusinessId is { } businessId
            && businessId != Guid.Empty && target.BusinessIds.Contains(businessId))
            return true;

        // InstitutionId eşleşme
        if (target.InstitutionId.HasValue && user.InstitutionId == target.InstitutionId.Value)
            return true;

        // Rol eşleşme (OR: herhangi biri yeterli) — bkz. sınıf özetindeki uyarı (#266)
        if (target.Roles is { Count: > 0 } && user.Roles.Any(r => target.Roles.Contains(r)))
            return true;

        // Permission eşleşme — bkz. sınıf özetindeki uyarı (#266)
        if (!string.IsNullOrEmpty(target.RequiredPermission) && user.Permissions.Contains(target.RequiredPermission))
            return true;

        return false;
    }
}
