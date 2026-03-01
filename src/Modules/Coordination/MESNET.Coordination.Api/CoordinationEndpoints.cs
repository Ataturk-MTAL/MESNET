using Marten;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Dtos;
using MESNET.Coordination.Application.Handlers;
using MESNET.Coordination.Application.Queries;
using MESNET.Coordination.Core.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Coordination.Api;

public static class CoordinationEndpoints
{
    public static IEndpointRouteBuilder MapCoordinationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/coordination/teachers")
            .WithTags("Coordination").RequireAuthorization();

        group.MapPost("/{teacherId:guid}/schedule", PostTeacherSchedule).RequireAuthorization(Permissions.Coordinator.Schedule);
        group.MapGet("/{teacherId:guid}/schedule", GetTeacherSchedule).RequireAuthorization(Permissions.Coordinator.Schedule);
        group.MapGet("/{teacherId:guid}/free-slots", GetTeacherFreeSlots).RequireAuthorization(Permissions.Coordinator.Schedule);
        group.MapPost("/{teacherId:guid}/assign-business", PostAssignBusiness).RequireAuthorization(Permissions.Coordinator.Schedule);
        group.MapGet("/{teacherId:guid}/workload", GetTeacherWorkload).RequireAuthorization(Permissions.DepartmentHead.Workload);

        // Coordination config + assignment endpoints
        group.MapGet("/config", GetConfig).RequireAuthorization(Permissions.DepartmentHead.Distribution);
        group.MapPost("/config", PostConfig).RequireAuthorization(Permissions.DepartmentHead.Distribution);
        group.MapGet("/assignments", ListAssignments).RequireAuthorization(Permissions.DepartmentHead.Distribution);
        group.MapPost("/assignments", PostAssignment).RequireAuthorization(Permissions.DepartmentHead.Distribution);
        group.MapPost("/assignments/{businessId:guid}/distance", PostManualDistance).RequireAuthorization(Permissions.DepartmentHead.Distribution);
        group.MapGet("/summary", GetSummary).RequireAuthorization(Permissions.DepartmentHead.Workload);
        group.MapPost("/recalculate-distances", PostRecalculateDistances).RequireAuthorization(Permissions.DepartmentHead.Distribution);

        return app;
    }

    private static async Task<IResult> PostTeacherSchedule(
        Guid teacherId,
        UpsertTeacherSchedule command,
        IMessageBus bus)
    {
        var @event = await bus.InvokeAsync<TeacherScheduleUpserted>(
            command with { TeacherId = teacherId });

        var message = @event.IsNew
            ? "Öğretmen ders programı oluşturuldu."
            : "Öğretmen ders programı güncellendi.";

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { scheduleId = @event.ScheduleId })
            .AddMessage(message)
            .Build());
    }

    private static IResult GetTeacherSchedule(
        Guid teacherId,
        int year,
        string semester,
        IQuerySession session)
    {
        var query = new GetTeacherSchedule(teacherId, year, semester);
        var schedule = GetTeacherScheduleHandler.Handle(query, session);

        return Results.Ok(ResponseBuilder.Success()
            .AddData(schedule)
            .Build());
    }

    private static IResult GetTeacherFreeSlots(
        Guid teacherId,
        int year,
        string semester,
        string? day,
        IQuerySession session)
    {
        var query = new GetTeacherFreeSlots(teacherId, year, semester, day);
        var freeSlots = GetTeacherFreeSlotsHandler.Handle(query, session);

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { freeSlots })
            .Build());
    }

    private static async Task<IResult> PostAssignBusiness(
        Guid teacherId,
        AssignBusinessToFreeSlot command,
        IMessageBus bus)
    {
        await bus.InvokeAsync(command with { TeacherId = teacherId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme öğretmene atandı.")
            .Build());
    }

    // ── Coordination Config ──

    private static async Task<IResult> GetConfig(
        IQuerySession session,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        var config = await session.LoadAsync<CoordinationConfig>(instId);

        return Results.Ok(ResponseBuilder.Success()
            .AddData(config ?? new CoordinationConfig { Id = instId, InstitutionId = instId })
            .Build());
    }

    private static async Task<IResult> PostConfig(
        UpsertCoordinationConfig command,
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        await bus.InvokeAsync(command with { InstitutionId = instId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Koordinatörlük ayarları güncellendi.")
            .Build());
    }

    // ── Assignments ──

    private static async Task<IResult> ListAssignments(
        string? branchCode,
        Guid? teacherId,
        bool? assignedOnly,
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        var query = new ListBusinessesForAssignment(
            instId, branchCode, teacherId, assignedOnly);

        var result = await bus.InvokeAsync<List<BusinessAssignmentDto>>(query);

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .Build());
    }

    private static async Task<IResult> PostAssignment(
        AssignBusinessToTeacher command,
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        await bus.InvokeAsync(command with { InstitutionId = instId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme öğretmene atandı.")
            .Build());
    }

    private static async Task<IResult> PostManualDistance(
        Guid businessId,
        SetBusinessManualDistance command,
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        await bus.InvokeAsync(command with
        {
            BusinessId = businessId,
            InstitutionId = instId,
        });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme mesafesi güncellendi.")
            .Build());
    }

    private static async Task<IResult> GetSummary(
        string? branchCode,
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        var query = new GetCoordinationSummary(instId, branchCode);
        var result = await bus.InvokeAsync<CoordinationSummaryDto>(query);

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .Build());
    }

    private static async Task<IResult> GetTeacherWorkload(
        Guid teacherId,
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        var query = new GetTeacherWorkload(teacherId, instId);
        var result = await bus.InvokeAsync<TeacherWorkloadDto>(query);

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .Build());
    }

    private static async Task<IResult> PostRecalculateDistances(
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        await bus.InvokeAsync(new RecalculateDistances(instId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Mesafeler yeniden hesaplandı.")
            .Build());
    }

    private static Guid GetInstitutionId(HttpContext http)
    {
        var claim = http.User.FindFirst("institution_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
