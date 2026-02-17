namespace MESNET.Common.Infrastructure.Security;

/// <summary>
/// Kullanıcının güncel permission bilgisini sağlar.
/// Common.Infrastructure'da interface tanımlanır, Security.Application'da implement edilir.
/// Bu sayede döngüsel bağımlılık önlenir.
/// PermissionClaimsTransformation tarafından kullanılır.
/// </summary>
public interface IUserPermissionProvider
{
    Task<UserPermissionInfo?> GetUserPermissionInfoAsync(string keycloakUserId);
}

public sealed record UserPermissionInfo(
    bool IsEnabled,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> DirectPermissions);
