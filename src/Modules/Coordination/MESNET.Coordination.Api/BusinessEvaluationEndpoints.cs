using MESNET.Common.Shared;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Queries;
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
        var evaluationId = await bus.InvokeAsync<Guid>(command);

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
        await bus.InvokeAsync(command with { EvaluationId = evaluationId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme değerlendirmesi güncellendi.")
            .Build());
    }

    private static async Task<IResult> Get(
        Guid evaluationId, IMessageBus bus)
    {
        var evaluation = await bus.InvokeAsync<BusinessEvaluation>(new GetBusinessEvaluation(evaluationId));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(evaluation)
            .Build());
    }

    private static async Task<IResult> GetAll(
        Guid? businessId, Guid? institutionId,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = false,
        IMessageBus bus = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<BusinessEvaluation>>(
            new ListBusinessEvaluations(businessId, institutionId)
            { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending });

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .Build());
    }
}
