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

        // Literal suffix route'ları önce kaydet (route çakışmasını önler)
        group.MapGet("/{teacherId:guid}/schedule/current", GetCurrentSchedule).RequireAuthorization(Permissions.Coordinator.Schedule);
        group.MapGet("/{teacherId:guid}/schedule/streams", GetScheduleStreams).RequireAuthorization(Permissions.Coordinator.Schedule);
        group.MapGet("/{teacherId:guid}/schedule/history", GetScheduleHistoryEndpoint).RequireAuthorization(Permissions.Coordinator.Schedule);
        group.MapPost("/{teacherId:guid}/schedule", PostTeacherSchedule).RequireAuthorization(Permissions.Coordinator.Schedule);
        group.MapGet("/{teacherId:guid}/schedule", GetTeacherSchedule).RequireAuthorization(Permissions.Coordinator.Schedule);
        group.MapGet("/{teacherId:guid}/free-slots", GetTeacherFreeSlots).RequireAuthorization(Permissions.Coordinator.Schedule);
        group.MapPost("/{teacherId:guid}/assign-business", PostAssignBusiness).RequireAuthorization(Permissions.Coordinator.Schedule);
        group.MapGet("/{teacherId:guid}/workload", GetTeacherWorkload).RequireAuthorization(Permissions.DepartmentHead.Workload);
        group.MapGet("/{teacherId:guid}/overview", GetTeacherOverview).RequireAuthorization(Permissions.DepartmentHead.Workload);

        // Coordination config + assignment endpoints
        group.MapGet("/config", GetConfig).RequireAuthorization(Permissions.DepartmentHead.Distribution);
        group.MapPost("/config", PostConfig).RequireAuthorization(Permissions.DepartmentHead.Distribution);
        group.MapGet("/assignments", ListAssignments).RequireAuthorization(Permissions.DepartmentHead.Distribution);
        group.MapPost("/assignments", PostAssignment).RequireAuthorization(Permissions.DepartmentHead.Distribution);
        group.MapDelete("/assignments/{businessId:guid}", DeleteAssignment).RequireAuthorization(Permissions.DepartmentHead.Distribution);
        group.MapPatch("/assignments/{businessId:guid}/hours", PatchAssignedHours).RequireAuthorization(Permissions.DepartmentHead.Distribution);
        group.MapDelete("/assignments/{businessId:guid}/slot", DeleteAssignmentSlot).RequireAuthorization(Permissions.DepartmentHead.Distribution);
        group.MapPost("/assignments/{businessId:guid}/distance", PostManualDistance).RequireAuthorization(Permissions.DepartmentHead.Distribution);
        group.MapGet("/assignments/{businessId:guid}/history", GetAssignmentHistory).RequireAuthorization(Permissions.DepartmentHead.Distribution);
        group.MapGet("/summary", GetSummary).RequireAuthorization(Permissions.DepartmentHead.Workload);
        group.MapGet("/overview-all", GetAllTeachersOverview).RequireAuthorization(Permissions.DepartmentHead.Workload);
        group.MapGet("/business-clusters", GetBusinessClusters).RequireAuthorization(Permissions.DepartmentHead.Distribution);
        group.MapPost("/recalculate-distances", PostRecalculateDistances).RequireAuthorization(Permissions.DepartmentHead.Distribution);

        // Branch workload config
        group.MapGet("/branch-workload/{branchCode}", GetBranchWorkload).RequireAuthorization(Permissions.DepartmentHead.Distribution);
        group.MapPut("/branch-workload/{branchCode}", PutBranchWorkload).RequireAuthorization(Permissions.DepartmentHead.Distribution);

        return app;
    }

    private static async Task<IResult> PostTeacherSchedule(
        Guid teacherId,
        UpsertTeacherSchedule command,
        IMessageBus bus)
    {
        var scheduleId = await bus.InvokeAsync<Guid>(
            command with { TeacherId = teacherId });

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { scheduleId })
            .AddMessage("Ders programı kaydedildi.")
            .Build());
    }

    private static async Task<IResult> GetTeacherSchedule(
        Guid teacherId,
        int year,
        string semester,
        IMessageBus bus)
    {
        var schedule = await bus.InvokeAsync<TeacherScheduleDto>(
            new GetTeacherSchedule(teacherId, year, semester));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(schedule)
            .Build());
    }

    private static async Task<IResult> GetCurrentSchedule(
        Guid teacherId,
        Guid academicPeriodId,
        string semester,
        IMessageBus bus)
    {
        var schedule = await bus.InvokeAsync<TeacherScheduleDto?>(
            new GetCurrentSchedule(teacherId, academicPeriodId, semester));

        if (schedule is null)
            return Results.NotFound(ResponseBuilder.Fail(404).AddMessage("Kayıtlı ders programı bulunamadı.").Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(schedule)
            .Build());
    }

    private static async Task<IResult> GetScheduleStreams(
        Guid teacherId,
        IMessageBus bus)
    {
        var streams = await bus.InvokeAsync<List<ScheduleStreamSummaryDto>>(
            new GetScheduleStreams(teacherId));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(streams)
            .Build());
    }

    private static async Task<IResult> GetScheduleHistoryEndpoint(
        Guid teacherId,
        Guid? scheduleId,
        IMessageBus bus)
    {
        var history = await bus.InvokeAsync<ScheduleHistoryDto?>(
            new GetScheduleHistory(teacherId, scheduleId));

        if (history is null)
            return Results.NotFound(ResponseBuilder.Fail(404).AddMessage("Program geçmişi bulunamadı.").Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(history)
            .Build());
    }

    private static async Task<IResult> GetTeacherFreeSlots(
        Guid teacherId,
        int year,
        string semester,
        string? day,
        IMessageBus bus)
    {
        var freeSlots = await bus.InvokeAsync<List<FreeSlotDto>>(
            new GetTeacherFreeSlots(teacherId, year, semester, day));

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
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        var config = await bus.InvokeAsync<CoordinationConfig>(new GetCoordinationConfig(instId));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(config)
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
        Guid? academicPeriodId,
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        var query = new ListBusinessesForAssignment(
            instId, branchCode, teacherId, assignedOnly, academicPeriodId);

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

    private static async Task<IResult> DeleteAssignment(
        Guid businessId,
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        var userName = http.User.FindFirst("name")?.Value ?? "system";
        await bus.InvokeAsync(new UnassignBusinessFromTeacher(businessId, instId, userName));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme ataması kaldırıldı.")
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

    private static async Task<IResult> GetAssignmentHistory(
        Guid businessId,
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        var result = await bus.InvokeAsync<List<AssignmentHistoryEntryDto>>(
            new GetAssignmentHistory(businessId, instId));

        return Results.Ok(ResponseBuilder.Success().AddData(result).Build());
    }

    private static async Task<IResult> GetSummary(
        string? branchCode,
        Guid? academicPeriodId,
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        var query = new GetCoordinationSummary(instId, branchCode, academicPeriodId);
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

    private static async Task<IResult> GetTeacherOverview(
        Guid teacherId,
        Guid academicPeriodId,
        string semester,
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        var query = new GetTeacherOverview(teacherId, instId, academicPeriodId, semester);
        var result = await bus.InvokeAsync<TeacherOverviewDto>(query);

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .Build());
    }

    private static async Task<IResult> GetAllTeachersOverview(
        Guid academicPeriodId,
        string semester,
        string? branchCode,
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        var query = new GetAllTeachersOverview(instId, academicPeriodId, semester, branchCode);
        var result = await bus.InvokeAsync<List<TeacherSummaryRowDto>>(query);

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .Build());
    }

    private static async Task<IResult> GetBusinessClusters(
        double epsMeters,
        int minPoints,
        string? branchCode,
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        var query = new GetBusinessClusters(instId, epsMeters, minPoints, branchCode);
        var result = await bus.InvokeAsync<List<BusinessClusterDto>>(query);

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .Build());
    }

    private static async Task<IResult> PatchAssignedHours(
        Guid businessId,
        UpdateBusinessAssignedHours command,
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        var userName = http.User.FindFirst("name")?.Value ?? "system";
        await bus.InvokeAsync(command with
        {
            BusinessId = businessId,
            InstitutionId = instId,
            UpdatedBy = userName
        });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Takdir edilen saat güncellendi.")
            .Build());
    }

    private static async Task<IResult> DeleteAssignmentSlot(
        Guid businessId,
        string day,
        int periodNumber,
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        var userName = http.User.FindFirst("name")?.Value ?? "system";
        await bus.InvokeAsync(new UnassignBusinessSlot(businessId, day, periodNumber, instId, userName));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme slot ataması kaldırıldı.")
            .Build());
    }

    // ── Branch Workload Config ──

    private static async Task<IResult> GetBranchWorkload(
        string branchCode,
        Guid academicPeriodId,
        string educationType,
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        var result = await bus.InvokeAsync<BranchWorkloadConfig?>(
            new GetBranchWorkloadConfig(instId, academicPeriodId, branchCode, educationType));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result ?? (object)new { })
            .Build());
    }

    private static async Task<IResult> PutBranchWorkload(
        string branchCode,
        UpsertBranchWorkloadConfig command,
        IMessageBus bus,
        HttpContext http)
    {
        var instId = GetInstitutionId(http);
        var userName = http.User.FindFirst("name")?.Value ?? "system";
        await bus.InvokeAsync(command with
        {
            InstitutionId = instId,
            BranchCode = branchCode,
            UpdatedBy = userName
        });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Alan ders yükü yapılandırması güncellendi.")
            .Build());
    }

    private static Guid GetInstitutionId(HttpContext http)
    {
        var claim = http.User.FindFirst("institution_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}
