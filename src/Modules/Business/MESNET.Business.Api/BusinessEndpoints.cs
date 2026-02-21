using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Dtos;
using MESNET.Business.Application.Queries;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Business.Api;

public static class BusinessEndpoints
{
    public static IEndpointRouteBuilder MapBusinessEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/businesses").WithTags("Business").RequireAuthorization();
        group.MapGet("/{businessId:guid}", Get).RequireAuthorization(Permissions.Company.View);
        group.MapPost("/", Post).RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/self-register", PostSelfRegister);  // Authenticate kullanıcı — CompanyManager kendi kaydeder
        group.MapPut("/{businessId:guid}", Put).RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/{businessId:guid}/approve", PostApprove).RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/{businessId:guid}/reject", PostReject).RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/{businessId:guid}/deactivate", PostDeactivate).RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/{businessId:guid}/activate", PostActivate).RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/{businessId:guid}/close", PostClose).RequireAuthorization(Permissions.Company.Manage);
        return app;
    }

    private static async Task<IResult> Get(Guid businessId, IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<BusinessDto>(new GetBusiness(businessId));
        return Results.Ok(ResponseBuilder.Success().AddData(dto).Build());
    }

    private static async Task<IResult> Post(RegisterBusiness command, IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<BusinessDto>(command);
        return Results.Created($"/api/businesses/{dto.Id}",
            ResponseBuilder.Success(201)
                .AddData(dto)
                .AddMessage("İşletme başarıyla kaydedildi.")
                .Build());
    }

    private static async Task<IResult> PostSelfRegister(SelfRegisterBusiness command, IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<BusinessDto>(command);
        return Results.Created($"/api/businesses/{dto.Id}",
            ResponseBuilder.Success(201)
                .AddData(dto)
                .AddMessage("İşletme kaydı alındı, onay bekleniyor.")
                .Build());
    }

    private static async Task<IResult> Put(Guid businessId, UpdateBusinessInfo command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { BusinessId = businessId });
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme bilgileri güncellendi.")
            .Build());
    }

    private static async Task<IResult> PostApprove(Guid businessId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ApproveBusiness(businessId));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme onaylandı.")
            .Build());
    }

    private static async Task<IResult> PostReject(Guid businessId, RejectBusiness command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { BusinessId = businessId });
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme reddedildi.")
            .Build());
    }

    private static async Task<IResult> PostDeactivate(Guid businessId, DeactivateBusiness command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { BusinessId = businessId });
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme pasife alındı.")
            .Build());
    }

    private static async Task<IResult> PostActivate(Guid businessId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ActivateBusiness(businessId));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme aktifleştirildi.")
            .Build());
    }

    private static async Task<IResult> PostClose(Guid businessId, IMessageBus bus)
    {
        await bus.InvokeAsync(new CloseBusiness(businessId));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme kapatıldı.")
            .Build());
    }
}
