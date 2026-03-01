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

public static class MonthlyActivityReportEndpoints
{
    public static IEndpointRouteBuilder MapMonthlyActivityReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/coordination/activity-reports")
            .WithTags("MonthlyActivityReport").RequireAuthorization();

        group.MapPost("/", Post).RequireAuthorization(Permissions.Coordinator.Report);
        group.MapPut("/{reportId:guid}", Put).RequireAuthorization(Permissions.Coordinator.Report);
        group.MapPost("/{reportId:guid}/submit", PostSubmit).RequireAuthorization(Permissions.Coordinator.Report);
        group.MapPost("/{reportId:guid}/approve", PostApprove).RequireAuthorization(Permissions.Internship.Manage);
        group.MapGet("/{reportId:guid}", Get).RequireAuthorization(Permissions.Coordinator.Report);
        group.MapGet("/", GetAll).RequireAuthorization(Permissions.Coordinator.Report);

        return app;
    }

    private static async Task<IResult> Post(
        CreateMonthlyActivityReport command, IMessageBus bus)
    {
        var reportId = await bus.InvokeAsync<Guid>(command);

        return Results.Created(
            $"/api/coordination/activity-reports/{reportId}",
            ResponseBuilder.Success(201)
                .AddData(new { reportId })
                .AddMessage("Aylık faaliyet raporu oluşturuldu.")
                .Build());
    }

    private static async Task<IResult> Put(
        Guid reportId, UpdateMonthlyActivityReport command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { ReportId = reportId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Aylık faaliyet raporu güncellendi.")
            .Build());
    }

    private static async Task<IResult> PostSubmit(
        Guid reportId, IMessageBus bus)
    {
        await bus.InvokeAsync(new SubmitMonthlyActivityReport(reportId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Aylık faaliyet raporu gönderildi.")
            .Build());
    }

    private static async Task<IResult> PostApprove(
        Guid reportId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ApproveMonthlyActivityReport(reportId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Aylık faaliyet raporu onaylandı.")
            .Build());
    }

    private static async Task<IResult> Get(
        Guid reportId, IMessageBus bus)
    {
        var report = await bus.InvokeAsync<MonthlyActivityReport>(new GetActivityReport(reportId));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(report)
            .Build());
    }

    private static async Task<IResult> GetAll(
        Guid? studentId, Guid? businessId, Guid? academicPeriodId, int? year, int? month,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = false,
        IMessageBus bus = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<MonthlyActivityReport>>(
            new ListActivityReports(studentId, businessId, academicPeriodId, year, month)
            { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending });

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .Build());
    }
}
