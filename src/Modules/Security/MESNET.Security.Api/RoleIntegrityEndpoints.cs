using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Security.Application.Commands;
using MESNET.Security.Application.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Security.Api;

/// <summary>
/// Rol modeli tutarlılık taraması (#129) — <b>yalnız tespit, düzeltme yok</b>.
/// </summary>
public static class RoleIntegrityEndpoints
{
    public static void MapRoleIntegrityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/security").WithTags("RoleIntegrity");

        // Raporu görmesi gereken kişi, düzeltmeyi de yapacak olandır → user:roles:manage.
        group.MapGet("/role-integrity", GetRoleIntegrity)
            .RequireAuthorization(Permissions.UserManagement.RolesManage);
    }

    /// <summary>
    /// Bozuk rol kaydı tespiti. Düzeltme <b>önerilir, uygulanmaz</b>: kimin müdür yardımcısı
    /// kimin personel olduğu okulun bilgisidir, kod tahmin edemez.
    /// </summary>
    private static async Task<IResult> GetRoleIntegrity(IMessageBus bus)
    {
        var report = await bus.InvokeAsync<RoleIntegrityReport>(new GetRoleIntegrityReport());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(report)
            .Build());
    }
}
