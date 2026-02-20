using Marten;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Handlers;
using MESNET.Coordination.Application.Queries;
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
}
