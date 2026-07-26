using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Security.Core.Entities;

namespace MESNET.Security.Application.Services;

/// <summary>
/// Marten'dan UserAccount okuyarak güncel permission bilgisini sağlar.
/// PermissionClaimsTransformation tarafından her istekte kullanılır (5dk cache'li).
/// </summary>
public sealed class UserPermissionProvider : IUserPermissionProvider
{
    private readonly IQuerySession _session;

    public UserPermissionProvider(IQuerySession session)
    {
        _session = session;
    }

    public async Task<UserPermissionInfo?> GetUserPermissionInfoAsync(string keycloakUserId)
    {
        var account = await _session.Query<UserAccount>()
            .Where(u => u.KeycloakUserId == keycloakUserId)
            .FirstOrDefaultAsync();

        if (account is null)
            return null;

        return new UserPermissionInfo(
            account.IsEnabled,
            account.Roles.AsReadOnly(),
            account.DirectPermissions.AsReadOnly(),
            account.BranchCodes.AsReadOnly());
    }
}
