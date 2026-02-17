using MESNET.Common.Shared.Security;
using Microsoft.AspNetCore.Authorization;

namespace MESNET.Common.Infrastructure.Security;

/// <summary>
/// Permission bazlı policy handler.
/// Kullanıcının "permissions" claim'lerinde ilgili permission var mı kontrol eder.
/// Wildcard desteği: "student:*" → "student:view" policy'si için geçerli.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var permissionClaims = context.User.FindAll("permissions").Select(c => c.Value);

        foreach (var claim in permissionClaims)
        {
            if (RolePermissionMap.MatchesPermission(claim, requirement.Permission))
            {
                context.Succeed(requirement);
                break;
            }
        }

        return Task.CompletedTask;
    }
}
