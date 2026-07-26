using MESNET.Common.Shared;

namespace MESNET.Security.Application.Services;

public interface IKeycloakAdminService
{
    Task<Result<string>> CreateUserAsync(
        string username, string email, string firstName, string lastName,
        string? temporaryPassword, CancellationToken ct = default);

    Task<Result> UpdateUserAsync(
        string keycloakUserId, string email, string firstName, string lastName,
        CancellationToken ct = default);

    Task<Result> SetUserEnabledAsync(
        string keycloakUserId, bool enabled, CancellationToken ct = default);

    Task<Result> DeleteUserAsync(
        string keycloakUserId, CancellationToken ct = default);

    Task<Result> AssignRealmRolesAsync(
        string keycloakUserId, IEnumerable<string> roles, CancellationToken ct = default);

    Task<Result> RemoveRealmRolesAsync(
        string keycloakUserId, IEnumerable<string> roles, CancellationToken ct = default);

    Task<Result> SetUserAttributesAsync(
        string keycloakUserId, Dictionary<string, string> attributes, CancellationToken ct = default);

    /// <summary>
    /// Çok değerli (multivalued) kullanıcı özniteliği yazar — ör. <c>branch_codes</c> (#126).
    /// Boş liste özniteliği <b>siler</b>; öznitelik zorunlu değildir, hiç bulunmaması geçerlidir.
    /// </summary>
    Task<Result> SetUserAttributeValuesAsync(
        string keycloakUserId, string attributeName, IReadOnlyList<string> values,
        CancellationToken ct = default);

    /// <summary>Realm'deki tüm kullanıcıları (rolleri + institution/business/branch attribute'larıyla) döndürür.</summary>
    Task<Result<List<KeycloakUserInfo>>> GetUsersAsync(CancellationToken ct = default);
}

/// <summary>Senkronizasyon için Keycloak'tan çekilen kullanıcı bilgisi.</summary>
public sealed record KeycloakUserInfo(
    string Id, string Username, string Email,
    string FirstName, string LastName, bool Enabled,
    List<string> Roles, Guid? InstitutionId, Guid? BusinessId,
    List<string> BranchCodes);
