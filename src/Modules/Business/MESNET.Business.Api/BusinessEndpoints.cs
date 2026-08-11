using MESNET.Business.Shared.Events;
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
        group.MapPatch("/{businessId:guid}", Patch).RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/{businessId:guid}/approve", PostApprove).RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/{businessId:guid}/reject", PostReject).RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/{businessId:guid}/deactivate", PostDeactivate).RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/{businessId:guid}/activate", PostActivate).RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/{businessId:guid}/close", PostClose).RequireAuthorization(Permissions.Company.Manage);
        // Okul kendi kapatma bildirimini geri çeker (#151); sayı eşiğin altına inerse işletme
        // kendiliğinden açılır.
        group.MapPost("/{businessId:guid}/close/retract", PostRetractClosure)
            .RequireAuthorization(Permissions.Company.Manage);
        group.MapPut("/{businessId:guid}/branch-authorizations", PutBranchAuthorizations)
            .RequireAuthorization(Permissions.Company.Manage);
        group.MapPost("/resync-projections", PostResyncProjections).RequireAuthorization(Permissions.Company.Manage);
        return app;
    }

    /// <summary>
    /// Tüm işletmeler için BusinessUpdated'ı yeniden yayınlar — diğer modüllerin denormalize
    /// işletme read-model'lerini tazeler. Read-model'e yeni alan eklendiğinde mevcut kayıtlar
    /// geriye dönük dolmadığı için gerekli (#77).
    /// </summary>
    private static async Task<IResult> PostResyncProjections(IMessageBus bus)
    {
        var result = await bus.InvokeAsync<ResyncBusinessProjectionsResult>(
            new ResyncBusinessProjections());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .AddMessage($"{result.BusinessCount} işletme için read-model'ler yeniden yayınlandı.")
            .Build());
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

    private static async Task<IResult> Patch(Guid businessId, UpdateBusinessInfo command, IMessageBus bus)
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

    /// <summary>
    /// İdare, belge incelemesi sonucunda işletmenin öğrenci alabileceği alanları işaretler (#119).
    /// Gövdedeki liste NİHAİ listedir — gönderilmeyen mevcut yetkiler iptal edilir.
    /// </summary>
    private static async Task<IResult> PutBranchAuthorizations(
        Guid businessId, AuthorizeBusinessForBranches command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { BusinessId = businessId });
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletmenin alan yetkileri güncellendi.")
            .Build());
    }

    /// <summary>
    /// Kapatma <b>bildirimi</b> (#151). Tek okulun bildirimi işletmeyi kapatmaz; işletme ancak
    /// farklı okullardan gelen bildirim sayısı yeter sayıya ulaşınca küresel olarak kapanır.
    /// Yanıt mesajı hangi durumun gerçekleştiğini söyler — "kapatıldı" demek, bildirimin
    /// beklemede olduğu durumda yanlış olurdu.
    /// </summary>
    private static async Task<IResult> PostClose(
        Guid businessId, CloseBusinessRequest? request, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<object>(new CloseBusiness(businessId, request?.Reason));

        var message = result is BusinessClosureReported reported
            ? $"Kapatma bildiriminiz alındı. Bildirimde bulunan kurum sayısı: {reported.ReportingInstitutionCount}. "
              + "İşletme, yeter sayıya ulaşıldığında kapanacaktır."
            : "İşletme kapatıldı.";

        return Results.Ok(ResponseBuilder.Success().AddData(result).AddMessage(message).Build());
    }

    private static async Task<IResult> PostRetractClosure(
        Guid businessId, CloseBusinessRequest? request, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<BusinessClosureRetracted>(
            new RetractBusinessClosure(businessId, request?.Reason));

        var message = result.Reopened
            ? "Bildiriminiz geri çekildi ve yeter sayı düştüğü için işletme yeniden açıldı."
            : $"Bildiriminiz geri çekildi. Kalan bildirimde bulunan kurum sayısı: {result.ReportingInstitutionCount}.";

        return Results.Ok(ResponseBuilder.Success().AddData(result).AddMessage(message).Build());
    }

    /// <summary>Kapatma bildirimi/geri çekme gerekçesi — isteğe bağlı.</summary>
    public sealed record CloseBusinessRequest(string? Reason);
}
