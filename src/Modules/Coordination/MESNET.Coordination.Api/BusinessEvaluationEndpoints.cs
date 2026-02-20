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

public static class BusinessEvaluationEndpoints
{
    public static IEndpointRouteBuilder MapBusinessEvaluationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/coordination/business-evaluations")
            .WithTags("BusinessEvaluation").RequireAuthorization();

        group.MapPost("/", Post).RequireAuthorization(Permissions.Coordinator.Visit);
        group.MapPut("/{evaluationId:guid}", Put).RequireAuthorization(Permissions.Coordinator.Visit);
        group.MapGet("/{evaluationId:guid}", Get).RequireAuthorization(Permissions.Coordinator.Visit);
        group.MapGet("/", GetAll).RequireAuthorization(Permissions.Coordinator.Visit);

        return app;
    }

    private static async Task<IResult> Post(
        CreateBusinessEvaluation command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result<Guid>>(command);

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        var evaluationId = result.Value;
        return Results.Created(
            $"/api/coordination/business-evaluations/{evaluationId}",
            ResponseBuilder.Success(201)
                .AddData(new { evaluationId })
                .AddMessage("İşletme değerlendirmesi oluşturuldu.")
                .Build());
    }

    private static async Task<IResult> Put(
        Guid evaluationId, UpdateBusinessEvaluation command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result>(command with { EvaluationId = evaluationId });

        if (result.IsFailure)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme değerlendirmesi güncellendi.")
            .Build());
    }

    private static async Task<IResult> Get(
        Guid evaluationId, IQuerySession session)
    {
        var evaluation = await session.LoadAsync<BusinessEvaluation>(evaluationId);
        if (evaluation is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage($"İşletme değerlendirmesi bulunamadı: {evaluationId}")
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(evaluation)
            .Build());
    }

    private static async Task<IResult> GetAll(
        Guid? businessId, Guid? institutionId,
        IQuerySession session)
    {
        IQueryable<BusinessEvaluation> queryable = session.Query<BusinessEvaluation>();

        if (businessId.HasValue)
            queryable = queryable.Where(e => e.BusinessId == businessId.Value);
        if (institutionId.HasValue)
            queryable = queryable.Where(e => e.InstitutionId == institutionId.Value);

        var evaluations = await queryable.ToListAsync();
        return Results.Ok(ResponseBuilder.Success()
            .AddData(evaluations)
            .Build());
    }
}
