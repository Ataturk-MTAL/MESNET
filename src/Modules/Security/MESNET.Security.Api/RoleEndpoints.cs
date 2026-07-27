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

        group.MapGet("/roles", GetRoles).RequireAuthorization(Permissions.UserManagement.View);
        group.MapGet("/roles/{roleName}/permissions", GetRolePermissions).RequireAuthorization(Permissions.UserManagement.View);
        group.MapGet("/permissions", GetAllPermissions).RequireAuthorization(Permissions.UserManagement.View);
    }

    /// <summary>
    /// Rol kataloğu — <b>arayüzün tek doğruluk kaynağı</b> (#129).
    ///
    /// <para>Türkçe etiket ve açıklama da buradan döner; istemci kendi rol listesini veya etiket
    /// haritasını tutmaz. Elle yazılmış liste, gerçek rollerle eşleşmeyi bırakıp uydurma adların
    /// (ör. <c>deputy_director</c>) sunucuya gitmesine yol açmıştı.</para>
    /// </summary>
    private static IResult GetRoles()
    {
        var roles = MesnetRoles.Catalog.Select(r => new
        {
            RoleName = r.Name,
            r.Label,
            r.Description,
            Permissions = RolePermissionMap.GetRawPermissionsForRole(r.Name)
        });

        return Results.Ok(ResponseBuilder.Success()
            .AddData(roles)
            .Build());
    }

    private static IResult GetRolePermissions(string roleName)
    {
        if (!MesnetRoles.IsValid(roleName))
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
