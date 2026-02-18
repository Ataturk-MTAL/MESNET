using Marten;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Core.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Coordination.Api;

public static class GuidanceVisitEndpoints
{
    public static IEndpointRouteBuilder MapGuidanceVisitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/coordination/guidance-visits")
            .WithTags("GuidanceVisit").RequireAuthorization();

        group.MapPost("/", Post).RequireAuthorization(Permissions.Coordinator.Visit);
        group.MapPut("/{visitId:guid}", Put).RequireAuthorization(Permissions.Coordinator.Visit);
        group.MapPost("/{visitId:guid}/submit", PostSubmit).RequireAuthorization(Permissions.Coordinator.Visit);
        group.MapPost("/{visitId:guid}/approve", PostApprove).RequireAuthorization(Permissions.Coordinator.Report);
        group.MapGet("/{visitId:guid}", Get).RequireAuthorization(Permissions.Coordinator.Visit);
        group.MapGet("/", GetAll).RequireAuthorization(Permissions.Coordinator.Visit);

        return app;
    }

    private static async Task<IResult> Post(
        CreateGuidanceVisit command, IMessageBus bus)
    {
        var (result, visitId) = await bus.InvokeAsync<(Result, Guid?)>(command);

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Created(
            $"/api/coordination/guidance-visits/{visitId}",
            ResponseBuilder.Success(201)
                .AddData(new { visitId })
                .AddMessage("Rehberlik ziyareti oluşturuldu.")
                .Build());
    }

    private static async Task<IResult> Put(
        Guid visitId, UpdateGuidanceVisit command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result>(command with { VisitId = visitId });

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Rehberlik ziyareti güncellendi.")
            .Build());
    }

    private static async Task<IResult> PostSubmit(
        Guid visitId, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result>(new SubmitGuidanceVisit(visitId));

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Rehberlik ziyareti gönderildi.")
            .Build());
    }

    private static async Task<IResult> PostApprove(
        Guid visitId, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result>(new ApproveGuidanceVisit(visitId));

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Rehberlik ziyareti onaylandı.")
            .Build());
    }

    private static async Task<IResult> Get(
        Guid visitId, IQuerySession session)
    {
        var visit = await session.LoadAsync<GuidanceVisit>(visitId);
        if (visit is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage($"Rehberlik ziyareti bulunamadı: {visitId}")
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(visit)
            .Build());
    }

    private static async Task<IResult> GetAll(
        Guid? teacherId, Guid? businessId, DateTime? fromDate, DateTime? toDate,
        IQuerySession session)
    {
        IQueryable<GuidanceVisit> queryable = session.Query<GuidanceVisit>();

        if (teacherId.HasValue)
            queryable = queryable.Where(v => v.TeacherId == teacherId.Value);
        if (businessId.HasValue)
            queryable = queryable.Where(v => v.BusinessId == businessId.Value);
        if (fromDate.HasValue)
            queryable = queryable.Where(v => v.VisitDate >= fromDate.Value);
        if (toDate.HasValue)
            queryable = queryable.Where(v => v.VisitDate <= toDate.Value);

        var visits = await queryable.ToListAsync();
        return Results.Ok(ResponseBuilder.Success()
            .AddData(visits)
            .Build());
    }
}
