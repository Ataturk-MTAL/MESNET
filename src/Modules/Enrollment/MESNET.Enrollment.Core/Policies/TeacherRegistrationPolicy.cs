using MESNET.Enrollment.Core.Entities;

namespace MESNET.Enrollment.Core.Policies;

/// <summary>
/// Öğretmen kaydının doğal anahtarı ve idempotanlık kuralı (#310) — saf (yan etkisiz) fonksiyon.
///
/// <para><b>Doğal anahtar <c>(InstitutionId, KeycloakUserId)</c>:</b> bir Keycloak kullanıcısının
/// aynı kurumda iki öğretmen profili olamaz. Aynı kişi başka okulda görevliyse orada AYRI profili
/// olur — kurum sınırı anahtarın parçasıdır.</para>
///
/// <para><b>Neden gerekti:</b> handler koşulsuz <c>Guid.NewGuid()</c> ile kayıt açıyordu; seeder'ın
/// ad bazlı kontrolü kiracı süzgeci yüzünden boş liste görünce (bkz. #309) her koşuda yeni profil
/// doğdu. Ölçüldü: 6 öğretmenin 2'şer kopyası, <c>/api/teachers</c> 12 satır.</para>
/// </summary>
public static class TeacherRegistrationPolicy
{
    /// <summary>
    /// Doğal anahtarla eşleşen mevcut profili döndürür; yoksa <c>null</c>.
    ///
    /// <para><b>Anahtarsız kayıt eşleşmez.</b> <c>KeycloakUserId</c> boşsa doğal anahtar yoktur;
    /// eşleştirmeye kalkışmak anahtarsız bütün öğretmenleri tek profile çökertirdi —
    /// mükerrerlikten beter bir sonuç.</para>
    ///
    /// <para><b>En eski kayıt kazanır.</b> Mevcut mükerrerler temizlenene kadar seçim KARARLI
    /// olmalıdır; yoksa atamalar iki profil arasında salınır.</para>
    /// </summary>
    public static TeacherProfile? FindExisting(
        IEnumerable<TeacherProfile> candidates,
        Guid institutionId,
        Guid keycloakUserId)
    {
        if (keycloakUserId == Guid.Empty) return null;

        return candidates
            .Where(t => t.InstitutionId == institutionId && t.KeycloakUserId == keycloakUserId)
            .OrderBy(t => t.RegisteredAt)
            .ThenBy(t => t.Id)
            .FirstOrDefault();
    }
}
