namespace MESNET.Institution.Shared.Events;

/// <param name="BranchCode">
/// Personelin alanı. <c>null</c> olabilir ve bu geçerlidir: müdür ve müdür yardımcısı
/// hiçbir alana bağlı değildir. Tüketiciler boş değeri "eksik veri" saymamalıdır (#126).
/// </param>
/// <param name="KeycloakId">
/// Personelin Keycloak kullanıcı kimliği (#126). Security modülü bu kimlikle kullanıcı
/// kaydını bulup alan (branş) kapsamını doldurabilsin diye event'e eklendi — modüller
/// arası doğrudan veri okuma yerine olay tabanlı yol.
/// </param>
public sealed record StaffAuthorized(
    Guid InstitutionId,
    Guid StaffMemberId,
    string Role,
    string? BranchCode,
    string KeycloakId = "");
