using MESNET.Common.Shared.Security;
using Microsoft.AspNetCore.Authorization;

namespace MESNET.Common.Infrastructure.Security;

/// <summary>
/// Permission bazlı policy handler.
/// Kullanıcının "permissions" claim'lerinde istenen izinlerden <b>herhangi biri</b> var mı
/// kontrol eder. Wildcard desteği: "student:*" → "student:view" policy'si için geçerli.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var permissionClaims = context.User.FindAll("permissions").Select(c => c.Value);

        foreach (var claim in permissionClaims)
        {
            // Herhangi biri yeterlidir: uç hem okul tarafına hem veri sahibine açık olabilir.
            // Hangi veriyi göreceğine handler'daki kapsam merdiveni karar verir (#182).
            if (requirement.Permissions.Any(required => RolePermissionMap.MatchesPermission(claim, required)))
            {
                context.Succeed(requirement);
                break;
            }
        }

        return Task.CompletedTask;
    }
}
