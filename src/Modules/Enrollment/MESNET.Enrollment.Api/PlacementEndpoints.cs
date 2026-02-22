using MESNET.Common.Shared;
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
        group.MapPost("/{placementId:guid}/transfer", PostTransfer).RequireAuthorization(Permissions.Internship.Manage);
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

    private static async Task<IResult> PostTransfer(
        Guid placementId, TransferStudent command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { PlacementId = placementId });
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Öğrenci transfer edildi.")
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
        Guid? businessId, Guid? studentId, Guid? academicPeriodId, string? status, IMessageBus bus)
    {
        var dtos = await bus.InvokeAsync<IReadOnlyList<InternshipPlacementDto>>(
            new ListPlacements(businessId, studentId, academicPeriodId, status));
        return Results.Ok(ResponseBuilder.Success().AddData(dtos).Build());
    }
}
