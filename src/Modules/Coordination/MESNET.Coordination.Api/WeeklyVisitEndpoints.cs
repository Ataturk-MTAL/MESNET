using MESNET.Common.Shared;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Dtos;
using MESNET.Coordination.Application.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Coordination.Api;

public static class WeeklyVisitEndpoints
{
    public static IEndpointRouteBuilder MapWeeklyVisitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/coordination/weekly-visits")
            .WithTags("WeeklyVisit").RequireAuthorization();

        group.MapPost("/generate", PostGenerate)
            .RequireAuthorization(Permissions.DepartmentHead.WeeklyVisit);

        group.MapDelete("/plans/{planId:guid}", Delete)
            .RequireAuthorization(Permissions.DepartmentHead.WeeklyVisit);

        group.MapGet("/plans", GetPlans)
            .RequireAuthorization(Permissions.DepartmentHead.WeeklyVisit);

        group.MapGet("/plans/{planId:guid}/assignments", GetAssignments)
            .RequireAuthorization(Permissions.DepartmentHead.WeeklyVisit);

        group.MapPost("/plans/{planId:guid}/assignments", PostAssignment)
            .RequireAuthorization(Permissions.DepartmentHead.WeeklyVisit);

        group.MapDelete("/plans/{planId:guid}/assignments/{assignmentId:guid}", DeleteAssignment)
            .RequireAuthorization(Permissions.DepartmentHead.WeeklyVisit);

        group.MapPost("/resync", PostResync)
            .RequireAuthorization(Permissions.Institution.Manage);

        return app;
    }

    private static async Task<IResult> PostGenerate(
        GenerateWeeklyVisitsRequest request, HttpContext http, IMessageBus bus)
    {
        var instId = GetInstitutionId(http);
        var generatedBy = GetUserName(http);

        var planId = await bus.InvokeAsync<Guid>(new GenerateWeeklyVisits(
            instId,
            request.AcademicPeriodId,
            request.Year,
            request.WeekNumber,
            request.Scope,
            request.TeacherId,
            request.BranchCode,
            generatedBy));

        return Results.Created(
            $"/api/coordination/weekly-visits/plans/{planId}",
            ResponseBuilder.Success(201)
                .AddData(new { planId })
                .AddMessage("Haftalık ziyaret planı oluşturuldu.")
                .Build());
    }

    private static async Task<IResult> Delete(
        Guid planId, HttpContext http, IMessageBus bus)
    {
        var instId = GetInstitutionId(http);

        await bus.InvokeAsync(new DeleteWeeklyVisitPlan(planId, instId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Haftalık ziyaret planı silindi.")
            .Build());
    }

    private static async Task<IResult> GetPlans(
        Guid? academicPeriodId, int? year, int? weekNumber,
        int page = 1, int pageSize = 20,
        string? sortBy = null, bool descending = false, string? search = null,
        HttpContext http = default!, IMessageBus bus = default!)
    {
        var instId = GetInstitutionId(http);

        var result = await bus.InvokeAsync<PagedResult<WeeklyVisitPlanDto>>(
            new ListWeeklyVisitPlans(instId, academicPeriodId, year, weekNumber)
            { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending, Search = search });

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .Build());
    }

    private static async Task<IResult> GetAssignments(
        Guid planId, Guid? teacherId, string? branchCode,
        int page = 1, int pageSize = 50,
        string? sortBy = null, bool descending = false, string? search = null,
        HttpContext http = default!, IMessageBus bus = default!)
    {
        var instId = GetInstitutionId(http);

        var result = await bus.InvokeAsync<PagedResult<WeeklyVisitAssignmentDto>>(
            new ListWeeklyVisitAssignments(planId, instId, teacherId, branchCode)
            { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending, Search = search });

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .Build());
    }

    private static async Task<IResult> PostAssignment(
        Guid planId, AddWeeklyVisitAssignmentRequest request, HttpContext http, IMessageBus bus)
    {
        var instId = GetInstitutionId(http);
        var addedBy = GetUserName(http);

        var assignmentId = await bus.InvokeAsync<Guid>(new AddWeeklyVisitAssignment(
            planId,
            instId,
            request.TeacherId,
            request.TeacherName,
            request.BusinessId,
            request.BusinessName,
            request.BranchCode,
            request.BranchName,
            request.Day,
            request.PeriodCount,
            addedBy));

        return Results.Created(
            $"/api/coordination/weekly-visits/plans/{planId}/assignments/{assignmentId}",
            ResponseBuilder.Success(201)
                .AddData(new { assignmentId })
                .AddMessage("Ziyaret ataması eklendi.")
                .Build());
    }

    private static async Task<IResult> DeleteAssignment(
        Guid planId, Guid assignmentId, HttpContext http, IMessageBus bus)
    {
        var instId = GetInstitutionId(http);

        await bus.InvokeAsync(new DeleteWeeklyVisitAssignment(planId, assignmentId, instId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Ziyaret ataması silindi.")
            .Build());
    }

    private static async Task<IResult> PostResync(
        ResyncWeeklyVisitsRequest request, HttpContext http, IMessageBus bus)
    {
        var instId = GetInstitutionId(http);

        var count = await bus.InvokeAsync<int>(new ResyncWeeklyVisitEvents(
            instId, request.AcademicPeriodId));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { resyncedAssignments = count })
            .AddMessage(count > 0
                ? $"{count} ziyaret ataması yeniden senkronize edildi."
                : "Senkronize edilecek ziyaret ataması bulunamadı.")
            .Build());
    }

    private static Guid GetInstitutionId(HttpContext http)
    {
        var claim = http.User.FindFirst("institution_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private static string GetUserName(HttpContext http)
    {
        return http.User.FindFirst("preferred_username")?.Value ?? "system";
    }
}

public sealed record GenerateWeeklyVisitsRequest(
    Guid AcademicPeriodId,
    int Year,
    int WeekNumber,
    string Scope,
    Guid? TeacherId,
    string? BranchCode);

public sealed record ResyncWeeklyVisitsRequest(
    Guid AcademicPeriodId);

public sealed record AddWeeklyVisitAssignmentRequest(
    Guid TeacherId,
    string TeacherName,
    Guid BusinessId,
    string BusinessName,
    string BranchCode,
    string BranchName,
    string Day,
    int PeriodCount);
