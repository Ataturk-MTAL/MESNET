using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace MESNET.Security.Api;

public static class RoleEndpoints
{
    public static void MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/security").WithTags("Role");

        group.MapGet("/roles", GetRoles).RequireAuthorization("user:view");
        group.MapGet("/roles/{roleName}/permissions", GetRolePermissions).RequireAuthorization("user:view");
        group.MapGet("/permissions", GetAllPermissions).RequireAuthorization("user:view");
    }

    private static IResult GetRoles()
    {
        var roles = MesnetRoles.All.Select(r => new
        {
            Name = r,
            Permissions = RolePermissionMap.GetRawPermissionsForRole(r)
        });

        return Results.Ok(ResponseBuilder.Success()
            .AddData(roles)
            .Build());
    }

    private static IResult GetRolePermissions(string roleName)
    {
        if (!MesnetRoles.All.Contains(roleName))
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage($"Rol bulunamadı: {roleName}")
                .Build());

        var permissions = RolePermissionMap.GetPermissionsForRoles([roleName]);

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { role = roleName, permissions })
            .Build());
    }

    private static IResult GetAllPermissions()
    {
        var permissions = Permissions.GetAll();

        return Results.Ok(ResponseBuilder.Success()
            .AddData(permissions)
            .Build());
    }
}
