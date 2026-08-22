namespace MESNET.Common.Infrastructure.Notifications;

/// <summary>
/// Bir bildirim, bağlı kullanıcıya <b>ulaşır mı</b> — saf karar (#247, #266).
///
/// <para><b>Ölçütler iki sınıfa ayrılır ve mantıkları FARKLIDIR.</b></para>
///
/// <list type="bullet">
///   <item><b>Tanımlayıcı ölçütler</b> — <c>UserIds</c>, <c>StudentIds</c>,
///   <c>GuardianOfStudentIds</c>, <c>BusinessIds</c>. Hepsi evrensel benzersiz kimliklerdir ve
///   kiracı sınırını <b>kendiliğinden</b> korurlar; aralarında OR çalışır.</item>
///
///   <item><b>Geniş ölçütler</b> — <c>Roles</c>, <c>RequiredPermission</c>. Çok kullanıcıya
///   uyarlar ve kiracı sınırını <b>KORUMAZLAR</b>; bu yüzden <c>InstitutionId</c> ile
///   <b>daraltılmak zorundadırlar</b>.</item>
/// </list>
///
/// <para><b>Ölçülmüş sızıntı (#266):</b> önceki sürümde beş dalın her biri ilk eşleşmede
/// <c>return true</c> diyordu — saf OR. <c>InstitutionId</c> ile birlikte konan bir
/// <c>Roles</c> ölçütü kurum süzgecini <b>tümden etkisiz</b> bırakıyordu: bir okulun belge
/// bildirimi <b>her okulun</b> müdürüne gidiyordu. Payment'ta daha kötüsü vardı —
/// <c>RequiredPermission = salary:approve</c> kurum süzgeci olmadan yayınlanıyor ve bir okulun
/// dekont gecikmesi, <b>öğrenci adı payload'da olacak şekilde</b>, tüm okulların onaycılarına
/// ulaşıyordu.</para>
///
/// <para><b>Kiracısız geniş hedef KİMSEYE ulaşmaz.</b> Bu bilinçli: sızdırmaktansa hiç
/// göndermemek doğrudur. Ama sessiz de kalmaz —
/// <see cref="NotificationTarget.LeaksWithoutInstitutionScope"/> ile
/// <see cref="SseNotificationService"/> uyarı yazar.</para>
///
/// <para><b>Neden saf politika:</b> karar önce <c>SseConnectionManager</c> içinde private bir
/// metottu ve <b>tek bir testi bile yoktu</b>. Hedefleme sessiz bir yüzeydir: hedef kitle boş
/// çıkarsa servis yalnız <c>LogDebug</c> yazar, yani yanlış hedefleme ne hata verir ne iz
/// bırakır.</para>
/// </summary>
public static class NotificationTargetPolicy
{
    public static bool Matches(SseUserContext user, NotificationTarget target)
    {
        if (MatchesIdentity(user, target)) return true;

        // Geniş ölçüt varsa karar TAMAMEN onun kurallarına göre verilir: kiracı zorunludur ve
        // eşleşmezse başka dala düşülmez.
        if (target.HasBroadCriteria)
            return MatchesInstitution(user, target) && MatchesBroad(user, target);

        // Yalnız kurum verilmişse: o kurumun tüm kullanıcıları.
        return MatchesInstitution(user, target);
    }

    /// <summary>
    /// Tanımlayıcı ölçütler — evrensel benzersiz kimlikler. Kiracı sınırını kendileri korur,
    /// bu yüzden <c>InstitutionId</c> ile daraltılmalarına gerek yoktur.
    /// </summary>
    private static bool MatchesIdentity(SseUserContext user, NotificationTarget target)
    {
        if (target.UserIds is { Count: > 0 } && target.UserIds.Contains(user.UserId))
            return true;

        // Öğrenci profili eşleşme — modüller öğrencinin Keycloak ID'sini bilmeden hedefleyebilsin
        // (#69). Yalnız öğrencinin KENDİSİNE ulaşır; velisi için GuardianOfStudentIds.
        if (target.StudentIds is { Count: > 0 } && user.StudentId.HasValue
            && target.StudentIds.Contains(user.StudentId.Value))
            return true;

        // Veli eşleşme (#174, #247) — kullanıcının bağlı olduğu öğrencilerden herhangi biri.
        if (target.GuardianOfStudentIds is { Count: > 0 }
            && user.LinkedStudents.Any(target.GuardianOfStudentIds.Contains))
            return true;

        // İşletme eşleşme (#247). business_id claim'i SUNUCU tarafından üretilir — token'daki
        // değer her istekte silinip UserAccount.BusinessId'den yeniden yazılır (#229).
        // Okulda stajda (#159) işletme yoktur; boş kimlik eşleşmez.
        if (target.BusinessIds is { Count: > 0 } && user.BusinessId is { } businessId
            && businessId != Guid.Empty && target.BusinessIds.Contains(businessId))
            return true;

        return false;
    }

    private static bool MatchesInstitution(SseUserContext user, NotificationTarget target)
        => target.InstitutionId.HasValue && user.InstitutionId == target.InstitutionId.Value;

    private static bool MatchesBroad(SseUserContext user, NotificationTarget target)
    {
        if (target.Roles is { Count: > 0 } && user.Roles.Any(target.Roles.Contains))
            return true;

        return !string.IsNullOrEmpty(target.RequiredPermission)
               && user.Permissions.Contains(target.RequiredPermission);
    }
}
