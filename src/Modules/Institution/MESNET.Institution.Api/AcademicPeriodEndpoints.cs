using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Institution.Api;

public static class AcademicPeriodEndpoints
{
    public static IEndpointRouteBuilder MapAcademicPeriodEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/institutions/{institutionId:guid}/academic-periods")
            .WithTags("AcademicPeriod").RequireAuthorization();

        group.MapGet("/", GetAll).RequireAuthorization(Permissions.Institution.View);
        group.MapGet("/active", GetActive).RequireAuthorization(Permissions.Institution.View);
        group.MapPost("/", Post).RequireAuthorization(Permissions.Institution.Manage);
        group.MapPost("/{periodId:guid}/close", PostClose).RequireAuthorization(Permissions.Institution.Manage);

        return app;
    }

    private static async Task<IResult> GetAll(Guid institutionId, IMessageBus bus)
    {
        var periods = await bus.InvokeAsync<IReadOnlyList<AcademicPeriodDto>>(
            new ListAcademicPeriods(institutionId));
        return Results.Ok(ResponseBuilder.Success()
            .AddData(periods)
            .Build());
    }

    private static async Task<IResult> GetActive(Guid institutionId, IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<AcademicPeriodDto>(
            new GetActiveAcademicPeriod(institutionId));
        return Results.Ok(ResponseBuilder.Success()
            .AddData(dto)
            .Build());
    }

    private static async Task<IResult> Post(
        Guid institutionId, CreateAcademicPeriod command, IMessageBus bus)
    {
        var periodId = await bus.InvokeAsync<Guid>(command with { InstitutionId = institutionId });
        return Results.Created(
            $"/api/institutions/{institutionId}/academic-periods/{periodId}",
            ResponseBuilder.Success(201)
                .AddData(new { id = periodId })
                .AddMessage("Akademik dönem oluşturuldu.")
                .Build());
    }

    private static async Task<IResult> PostClose(
        Guid institutionId, Guid periodId, IMessageBus bus)
    {
        await bus.InvokeAsync(new CloseAcademicPeriod(periodId));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Dönem kapatıldı.")
            .Build());
    }
}
