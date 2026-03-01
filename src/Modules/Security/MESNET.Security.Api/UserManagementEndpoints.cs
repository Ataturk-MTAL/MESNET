using MESNET.Common.Shared;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using MESNET.Security.Application.Commands;
using MESNET.Security.Application.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Security.Api;

public static class UserManagementEndpoints
{
    public static void MapUserManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/security/users").WithTags("UserManagement");

        group.MapPost("/", CreateUser).RequireAuthorization(Permissions.UserManagement.Create);
        group.MapGet("/", GetUsers).RequireAuthorization(Permissions.UserManagement.View);
        group.MapGet("/{userAccountId:guid}", GetUser).RequireAuthorization(Permissions.UserManagement.View);
        group.MapPut("/{userAccountId:guid}", UpdateUser).RequireAuthorization(Permissions.UserManagement.Update);
        group.MapPost("/{userAccountId:guid}/roles", ChangeRoles).RequireAuthorization(Permissions.UserManagement.RolesManage);
        group.MapPost("/{userAccountId:guid}/permissions", ChangePermissions).RequireAuthorization(Permissions.UserManagement.RolesManage);
        group.MapPost("/{userAccountId:guid}/toggle-status", ToggleStatus).RequireAuthorization(Permissions.UserManagement.Update);
        group.MapDelete("/{userAccountId:guid}", DeleteUser).RequireAuthorization(Permissions.UserManagement.Delete);
    }

    private static async Task<IResult> CreateUser(
        CreateUser command, IMessageBus bus)
    {
        var userId = await bus.InvokeAsync<Guid>(command);

        return Results.Created(
            $"/api/security/users/{userId}",
            ResponseBuilder.Success(201)
                .AddData(new { userId })
                .AddMessage("Kullanıcı oluşturuldu.")
                .Build());
    }

    private static async Task<IResult> GetUsers(
        Guid? institutionId, Guid? businessId, string? role, bool? isEnabled,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = false, string? search = null,
        IMessageBus bus = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<UserAccountDto>>(
            new GetUserAccounts(institutionId, businessId, role, isEnabled)
            { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending, Search = search });

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .Build());
    }

    private static async Task<IResult> GetUser(
        Guid userAccountId, IMessageBus bus)
    {
        var user = await bus.InvokeAsync<UserAccountDto>(new GetUserAccount(userAccountId));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(user)
            .Build());
    }

    private static async Task<IResult> UpdateUser(
        Guid userAccountId, UpdateUser command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { UserAccountId = userAccountId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kullanıcı güncellendi.")
            .Build());
    }

    private static async Task<IResult> ChangeRoles(
        Guid userAccountId, ChangeUserRoles command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { UserAccountId = userAccountId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kullanıcı rolleri güncellendi.")
            .Build());
    }

    private static async Task<IResult> ChangePermissions(
        Guid userAccountId, ChangeUserPermissions command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { UserAccountId = userAccountId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kullanıcı yetkileri güncellendi.")
            .Build());
    }

    private static async Task<IResult> ToggleStatus(
        Guid userAccountId, ToggleUserStatus command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { UserAccountId = userAccountId });

        var action = command.Enable ? "aktif" : "pasif";
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage($"Kullanıcı {action} yapıldı.")
            .Build());
    }

    private static async Task<IResult> DeleteUser(
        Guid userAccountId, IMessageBus bus)
    {
        await bus.InvokeAsync(new DeleteUser(userAccountId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kullanıcı silindi.")
            .Build());
    }
}
