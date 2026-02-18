using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Security.Application.Commands;
using MESNET.Security.Application.Handlers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Security.Api;

public static class InvitationEndpoints
{
    public static void MapInvitationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/security/invitations").WithTags("Invitation");

        group.MapPost("/", CreateInvitation).RequireAuthorization(Permissions.UserManagement.Create);
        group.MapGet("/", GetInvitations).RequireAuthorization(Permissions.UserManagement.View);
        group.MapPost("/{invitationId:guid}/approve", ApproveInvitation).RequireAuthorization(Permissions.UserManagement.Approve);
        group.MapPost("/{invitationId:guid}/reject", RejectInvitation).RequireAuthorization(Permissions.UserManagement.Approve);
        group.MapPost("/{invitationId:guid}/complete", CompleteInvitation).AllowAnonymous();
        group.MapPost("/{invitationId:guid}/resend", ResendInvitation).RequireAuthorization(Permissions.UserManagement.Create);
    }

    private static async Task<IResult> CreateInvitation(
        CreateInvitation command, IMessageBus bus)
    {
        var (result, _) = await bus.InvokeAsync<(Result<Guid>, object?)>(command);

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Created(
            $"/api/security/invitations/{result.Value}",
            ResponseBuilder.Success(201)
                .AddData(new { invitationId = result.Value })
                .AddMessage("Davet oluşturuldu.")
                .Build());
    }

    private static async Task<IResult> GetInvitations(
        Guid? institutionId, string? status, string? targetRole,
        IMessageBus bus)
    {
        var query = new GetInvitations(institutionId, status, targetRole);
        var invitations = await bus.InvokeAsync<IReadOnlyList<InvitationDto>>(query);

        return Results.Ok(ResponseBuilder.Success()
            .AddData(invitations)
            .Build());
    }

    private static async Task<IResult> ApproveInvitation(
        Guid invitationId, ApproveInvitation command, IMessageBus bus)
    {
        var (result, _) = await bus.InvokeAsync<(Result, object?)>(
            command with { InvitationId = invitationId });

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Davet onaylandı. Kayıt bağlantısı e-posta ile gönderildi.")
            .Build());
    }

    private static async Task<IResult> RejectInvitation(
        Guid invitationId, RejectInvitation command, IMessageBus bus)
    {
        var (result, _) = await bus.InvokeAsync<(Result, object?)>(
            command with { InvitationId = invitationId });

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Davet reddedildi.")
            .Build());
    }

    private static async Task<IResult> CompleteInvitation(
        Guid invitationId, CompleteInvitation command, IMessageBus bus)
    {
        var (result, _) = await bus.InvokeAsync<(Result, object[]?)>(
            command with { InvitationId = invitationId });

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kayıt tamamlandı. Artık giriş yapabilirsiniz.")
            .Build());
    }

    private static async Task<IResult> ResendInvitation(
        Guid invitationId, ResendInvitation command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result>(
            command with { InvitationId = invitationId });

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Davet e-postası yeniden gönderildi.")
            .Build());
    }
}
