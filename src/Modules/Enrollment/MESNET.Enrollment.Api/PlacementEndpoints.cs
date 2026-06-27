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
}
