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
        var invitationId = await bus.InvokeAsync<Guid>(command);

        return Results.Created(
            $"/api/security/invitations/{invitationId}",
            ResponseBuilder.Success(201)
                .AddData(new { invitationId })
                .AddMessage("Davet oluşturuldu.")
                .Build());
    }

    private static async Task<IResult> GetInvitations(
        Guid? institutionId, string? status, string? targetRole,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = false, string? search = null,
        IMessageBus bus = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<InvitationDto>>(
            new GetInvitations(institutionId, status, targetRole)
            { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending, Search = search });

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .Build());
    }

    private static async Task<IResult> ApproveInvitation(
        Guid invitationId, ApproveInvitation command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { InvitationId = invitationId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Davet onaylandı. Kayıt bağlantısı e-posta ile gönderildi.")
            .Build());
    }

    private static async Task<IResult> RejectInvitation(
        Guid invitationId, RejectInvitation command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { InvitationId = invitationId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Davet reddedildi.")
            .Build());
    }

    private static async Task<IResult> CompleteInvitation(
        Guid invitationId, CompleteInvitation command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { InvitationId = invitationId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kayıt tamamlandı. Artık giriş yapabilirsiniz.")
            .Build());
    }

    private static async Task<IResult> ResendInvitation(
        Guid invitationId, ResendInvitation command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { InvitationId = invitationId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Davet e-postası yeniden gönderildi.")
            .Build());
    }
}
