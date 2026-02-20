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
        Guid reportId, IQuerySession session)
    {
        var report = await session.LoadAsync<MonthlyActivityReport>(reportId);
        if (report is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage($"Aylık faaliyet raporu bulunamadı: {reportId}")
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(report)
            .Build());
    }

    private static async Task<IResult> GetAll(
        Guid? studentId, Guid? businessId, int? year, int? month,
        IQuerySession session)
    {
        IQueryable<MonthlyActivityReport> queryable = session.Query<MonthlyActivityReport>();

        if (studentId.HasValue)
            queryable = queryable.Where(r => r.StudentId == studentId.Value);
        if (businessId.HasValue)
            queryable = queryable.Where(r => r.BusinessId == businessId.Value);
        if (year.HasValue)
            queryable = queryable.Where(r => r.Year == year.Value);
        if (month.HasValue)
            queryable = queryable.Where(r => r.Month == month.Value);

        var reports = await queryable.ToListAsync();
        return Results.Ok(ResponseBuilder.Success()
            .AddData(reports)
            .Build());
    }
}
