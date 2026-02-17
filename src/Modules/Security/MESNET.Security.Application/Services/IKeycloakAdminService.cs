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
}
