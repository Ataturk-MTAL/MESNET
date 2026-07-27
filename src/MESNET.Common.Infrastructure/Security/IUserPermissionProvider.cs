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

/// <param name="BranchCodes">
/// Kullanıcı kaydında girilen alan (branş) kodları (#126). Kayıt sırasında sabitlenir;
/// boş olabilir ve bu bir eksiklik değildir (müdür/müdür yardımcısı alana bağlı değildir).
/// </param>
public sealed record UserPermissionInfo(
    bool IsEnabled,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> DirectPermissions,
    IReadOnlyList<string> BranchCodes);
