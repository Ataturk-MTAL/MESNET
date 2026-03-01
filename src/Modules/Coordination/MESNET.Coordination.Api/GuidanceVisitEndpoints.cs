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
        var visitId = await bus.InvokeAsync<Guid>(command);

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
        await bus.InvokeAsync(command with { VisitId = visitId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Rehberlik ziyareti güncellendi.")
            .Build());
    }

    private static async Task<IResult> PostSubmit(
        Guid visitId, IMessageBus bus)
    {
        await bus.InvokeAsync(new SubmitGuidanceVisit(visitId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Rehberlik ziyareti gönderildi.")
            .Build());
    }

    private static async Task<IResult> PostApprove(
        Guid visitId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ApproveGuidanceVisit(visitId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Rehberlik ziyareti onaylandı.")
            .Build());
    }

    private static async Task<IResult> Get(
        Guid visitId, IMessageBus bus)
    {
        var visit = await bus.InvokeAsync<GuidanceVisit>(new GetGuidanceVisit(visitId));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(visit)
            .Build());
    }

    private static async Task<IResult> GetAll(
        Guid? teacherId, Guid? businessId, Guid? academicPeriodId, DateTime? fromDate, DateTime? toDate,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = false, string? search = null,
        IMessageBus bus = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<GuidanceVisit>>(
            new ListGuidanceVisits(teacherId, businessId, academicPeriodId, fromDate, toDate)
            { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending, Search = search });

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .Build());
    }
}
