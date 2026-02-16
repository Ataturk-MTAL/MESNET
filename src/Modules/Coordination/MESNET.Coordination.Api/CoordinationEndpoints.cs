using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Handlers;
using MESNET.Coordination.Application.Queries;
using Microsoft.AspNetCore.Http;
using Wolverine;
using Wolverine.Http;

namespace MESNET.Coordination.Api;

public static class CoordinationEndpoints
{
    [WolverinePost("/api/coordination/teachers/{teacherId}/schedule")]
    public static async Task<IResult> PostTeacherSchedule(
        Guid teacherId,
        UpsertTeacherSchedule command,
        IMessageBus bus)
    {
        var (result, @event) = await bus.InvokeAsync<(Result, TeacherScheduleUpserted)>(
            command with { TeacherId = teacherId });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        var message = @event.IsNew
            ? "Öğretmen ders programı oluşturuldu."
            : "Öğretmen ders programı güncellendi.";

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { scheduleId = @event.ScheduleId })
            .AddMessage(message)
            .Build());
    }

    [WolverineGet("/api/coordination/teachers/{teacherId}/schedule")]
    public static IResult GetTeacherSchedule(
        Guid teacherId,
        int year,
        string semester,
        IQuerySession session)
    {
        var query = new GetTeacherSchedule(teacherId, year, semester);
        var result = GetTeacherScheduleHandler.Handle(query, session);

        if (result.IsFailure)
        {
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result.Value)
            .Build());
    }

    [WolverineGet("/api/coordination/teachers/{teacherId}/free-slots")]
    public static IResult GetTeacherFreeSlots(
        Guid teacherId,
        int year,
        string semester,
        string? day,
        IQuerySession session)
    {
        var query = new GetTeacherFreeSlots(teacherId, year, semester, day);
        var result = GetTeacherFreeSlotsHandler.Handle(query, session);

        if (result.IsFailure)
        {
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { freeSlots = result.Value })
            .Build());
    }

    [WolverinePost("/api/coordination/teachers/{teacherId}/assign-business")]
    public static async Task<IResult> PostAssignBusiness(
        Guid teacherId,
        AssignBusinessToFreeSlot command,
        IMessageBus bus)
    {
        var (result, @event) = await bus.InvokeAsync<(Result, BusinessAssignedToTeacher)>(
            command with { TeacherId = teacherId });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme öğretmene atandı.")
            .Build());
    }
}
