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
        ICurrentUserService currentUser = default!, IMessageBus bus = default!)
    {
        var user = currentUser.GetCurrentUser();
        var institutionId = user?.InstitutionId;
        Guid? teacherId = null;
        var effectiveBusinessId = businessId;

        // Teacher: sadece koordine ettiği öğrencilerin yerleştirmelerini görsün
        if (currentUser.IsInRole(MesnetRoles.Teacher)
            && !currentUser.IsInRole(MesnetRoles.InstitutionManager)
            && !currentUser.IsInRole(MesnetRoles.InstitutionStaff))
        {
            teacherId = await bus.InvokeAsync<Guid?>(new ResolveTeacherId(user!.UserId));
        }

        // CompanyManager: sadece kendi işletmesindeki yerleştirmeleri görsün
        if (currentUser.IsInRole(MesnetRoles.CompanyManager) && user?.BusinessId.HasValue == true)
        {
            effectiveBusinessId = user.BusinessId;
        }

        var result = await bus.InvokeAsync<PagedResult<InternshipPlacementDto>>(
            new ListPlacements(effectiveBusinessId, studentId, academicPeriodId, status, institutionId, teacherId, branchCode)
            { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending, Search = search });
        return Results.Ok(ResponseBuilder.Success().AddData(result).Build());
    }
}
