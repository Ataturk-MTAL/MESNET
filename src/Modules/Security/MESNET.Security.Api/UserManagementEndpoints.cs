using MESNET.Common.Shared;
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
        var (result, @event) = await bus.InvokeAsync<(Result<Guid>, object?)>(command);

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Created(
            $"/api/security/users/{result.Value}",
            ResponseBuilder.Success(201)
                .AddData(new { userId = result.Value })
                .AddMessage("Kullanıcı oluşturuldu.")
                .Build());
    }

    private static async Task<IResult> GetUsers(
        Guid? institutionId, Guid? businessId, string? role, bool? isEnabled,
        IMessageBus bus)
    {
        var query = new GetUserAccounts(institutionId, businessId, role, isEnabled);
        var users = await bus.InvokeAsync<IReadOnlyList<UserAccountDto>>(query);

        return Results.Ok(ResponseBuilder.Success()
            .AddData(users)
            .Build());
    }

    private static async Task<IResult> GetUser(
        Guid userAccountId, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result<UserAccountDto>>(new GetUserAccount(userAccountId));

        if (result.IsFailure)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result.Value)
            .Build());
    }

    private static async Task<IResult> UpdateUser(
        Guid userAccountId, UpdateUser command, IMessageBus bus)
    {
        var (result, _) = await bus.InvokeAsync<(Result, object?)>(
            command with { UserAccountId = userAccountId });

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kullanıcı güncellendi.")
            .Build());
    }

    private static async Task<IResult> ChangeRoles(
        Guid userAccountId, ChangeUserRoles command, IMessageBus bus)
    {
        var (result, _) = await bus.InvokeAsync<(Result, object?)>(
            command with { UserAccountId = userAccountId });

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kullanıcı rolleri güncellendi.")
            .Build());
    }

    private static async Task<IResult> ChangePermissions(
        Guid userAccountId, ChangeUserPermissions command, IMessageBus bus)
    {
        var (result, _) = await bus.InvokeAsync<(Result, object?)>(
            command with { UserAccountId = userAccountId });

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kullanıcı yetkileri güncellendi.")
            .Build());
    }

    private static async Task<IResult> ToggleStatus(
        Guid userAccountId, ToggleUserStatus command, IMessageBus bus)
    {
        var (result, _) = await bus.InvokeAsync<(Result, object?)>(
            command with { UserAccountId = userAccountId });

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        var action = command.Enable ? "aktif" : "pasif";
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage($"Kullanıcı {action} yapıldı.")
            .Build());
    }

    private static async Task<IResult> DeleteUser(
        Guid userAccountId, IMessageBus bus)
    {
        var (result, _) = await bus.InvokeAsync<(Result, object?)>(new DeleteUser(userAccountId));

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kullanıcı silindi.")
            .Build());
    }
}
