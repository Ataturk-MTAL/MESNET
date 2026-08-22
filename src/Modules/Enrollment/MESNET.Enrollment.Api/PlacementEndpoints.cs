using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Enrollment.Api;

public static class PlacementEndpoints
{
    public static IEndpointRouteBuilder MapPlacementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/placements").RequireAuthorization();

        group.MapPost("/", Post).RequireAuthorization(Permissions.Internship.Approve);
        group.MapPost("/{placementId:guid}/mark-failed", PostMarkFailed).RequireAuthorization(Permissions.Internship.Manage);
        group.MapPost("/resync-projections", PostResyncProjections).RequireAuthorization(Permissions.Internship.Manage);
        group.MapPost("/backfill-branch-authorizations", PostBackfillBranchAuthorizations)
            .RequireAuthorization(Permissions.Internship.Manage);
        group.MapGet("/status-counts", GetStatusCounts).RequireAuthorization(Permissions.Student.View);
        group.MapGet("/{placementId:guid}", Get).RequireAuthorization(Permissions.Student.View);
        group.MapGet("/", GetAll).RequireAuthorization(Permissions.Student.View);

        return app;
    }

    private static async Task<IResult> Post(PlaceStudent command, IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<InternshipPlacementDto>(command);
        return Results.Created($"/api/placements/{dto.Id}",
            ResponseBuilder.Success(201)
                .AddData(dto)
                .AddMessage("Öğrenci yerleştirildi.")
                .Build());
    }

    private static async Task<IResult> PostMarkFailed(
        Guid placementId, ICurrentUserService currentUser, IMessageBus bus)
    {
        var institutionId = currentUser.GetCurrentUser()?.InstitutionId
            ?? throw new DomainException(new Error("Auth.NoInstitution", "Kurum bilgisi bulunamadı."));
        await bus.InvokeAsync(new MarkAsFailedToComplete(placementId, institutionId));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Staj 'Tamamlayamadı' olarak işaretlendi.")
            .Build());
    }

    /// <summary>
    /// Sonlanmamış yerleştirmeler için StudentPlaced'i yeniden yayınlar — diğer modüllerin
    /// denormalize yerleştirme read-model'lerini tazeler. Yeni bir read-model eklendiğinde
    /// mevcut kayıtlar geriye dönük dolmadığı için gerekli (#77).
    /// </summary>
    private static async Task<IResult> PostResyncProjections(IMessageBus bus)
    {
        var result = await bus.InvokeAsync<ResyncPlacementProjectionsResult>(
            new ResyncPlacementProjections());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .AddMessage($"{result.PlacementCount} yerleştirme için read-model'ler yeniden yayınlandı." +
                        (result.Skipped > 0 ? $" {result.Skipped} kayıt eksik veri nedeniyle atlandı." : ""))
            .Build());
    }

    /// <summary>
    /// Geçiş dolgusu (#119): mevcut fiilî yerleştirmelerden işletmelerin alan yetkilerini üretir.
    /// Alan yetkisi kuralı devreye alınırken bir kez çalıştırılır; tekrar çalıştırmak güvenlidir.
    /// </summary>
    private static async Task<IResult> PostBackfillBranchAuthorizations(IMessageBus bus)
    {
        var result = await bus.InvokeAsync<BackfillBusinessBranchAuthorizationsResult>(
            new BackfillBusinessBranchAuthorizations());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .AddMessage($"{result.BusinessCount} işletme için {result.BranchCount} alan yetkisi dolgusu yayınlandı.")
            .Build());
    }

    private static async Task<IResult> Get(Guid placementId, IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<InternshipPlacementDto?>(new GetPlacement(placementId));
        if (dto is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage($"Yerleştirme bulunamadı: {placementId}").Build());

        return Results.Ok(ResponseBuilder.Success().AddData(dto).Build());
    }

    private static async Task<IResult> GetAll(
        Guid? businessId, Guid? studentId, Guid? academicPeriodId, string? status, string? branchCode,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = false, string? search = null,
        IMessageBus bus = default!)
    {
        // Yetki-kapsam daraltma (kurum + Teacher/CompanyManager) ListPlacementsHandler içinde
        // ICurrentUserService'ten türetilir — endpoint yalnız ham filtreleri geçer (ince adaptör).
        var result = await bus.InvokeAsync<PagedResult<InternshipPlacementDto>>(
            new ListPlacements(businessId, studentId, academicPeriodId, status, branchCode)
            { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending, Search = search });
        return Results.Ok(ResponseBuilder.Success().AddData(result).Build());
    }

    // Overview kartları için durum-bazında TOPLAM sayım (sayfalamadan bağımsız). Kapsam liste ile aynı.
    private static async Task<IResult> GetStatusCounts(
        Guid? academicPeriodId, string? branchCode, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<PlacementStatusCountsResult>(
            new GetPlacementStatusCounts(academicPeriodId, branchCode));
        return Results.Ok(ResponseBuilder.Success().AddData(result.Counts).Build());
    }
}
