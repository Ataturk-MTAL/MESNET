using MESNET.Security.Core.Entities;
using MESNET.Security.Shared.Events;

namespace MESNET.Security.Application.Events;

/// <summary>
/// <see cref="UserDisplayNameUpserted"/> üretimi tek yerde (#137).
///
/// <para>Olay dört yerden yayınlanır — kullanıcı oluşturma, güncelleme, davet tamamlama ve
/// Keycloak senkronizasyonu — ve hepsinin aynı ayrıştırma kuralına uyması gerekir.</para>
/// </summary>
public static class UserDisplayNameEvents
{
    /// <summary>
    /// Hesabın kimliği Guid olarak ayrıştırılamıyorsa <c>null</c> döner ve olay yayınlanmaz.
    /// Denetim alanı token'daki <c>sub</c> claim'ini saklar; ayrıştırılamayan bir kimlik için
    /// üretilecek view kaydı hiçbir denetim satırıyla eşleşmez, yalnız çöp olurdu.
    /// </summary>
    public static UserDisplayNameUpserted? TryCreate(string keycloakUserId, string fullName) =>
        Guid.TryParse(keycloakUserId, out var userId)
            ? new UserDisplayNameUpserted(userId, fullName)
            : null;

    /// <inheritdoc cref="TryCreate(string, string)"/>
    public static UserDisplayNameUpserted? TryCreate(UserAccount account) =>
        TryCreate(account.KeycloakUserId, account.FullName);
}
