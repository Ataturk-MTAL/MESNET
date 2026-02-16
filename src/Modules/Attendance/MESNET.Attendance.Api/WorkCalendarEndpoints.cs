using Marten;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Application.Extensions;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Shared;
using Microsoft.AspNetCore.Http;
using Wolverine;
using Wolverine.Http;

namespace MESNET.Attendance.Api;

public static class WorkCalendarEndpoints
{
    [WolverinePost("/api/work-calendar")]
    public static async Task<IResult> Post(
        UpdateWorkCalendar command, IMessageBus bus)
    {
        try
        {
            var @event = await bus.InvokeAsync<WorkCalendarUpdated>(command);
            return Results.Ok(ResponseBuilder.Success()
                .AddData(new { calendarId = @event.CalendarId })
                .AddMessage("Çalışma takvimi güncellendi.")
                .Build());
        }
        catch (InvalidOperationException ex)
        {
            var error = AttendanceErrors.OperationFailed("UpdateCalendar", ex.Message);
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(error.Description)
                .AddErrors(error)
                .Build());
        }
    }

    [WolverineGet("/api/work-calendar")]
    public static async Task<IResult> Get(
        Guid institutionId, int year, IQuerySession session)
    {
        var calendar = await session.Query<WorkCalendar>()
            .FirstOrDefaultAsync(c => c.InstitutionId == institutionId && c.Year == year);

        if (calendar is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(AttendanceErrors.CalendarNotFound(institutionId, year).Description)
                .AddErrors(AttendanceErrors.CalendarNotFound(institutionId, year))
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(calendar.ToDto())
            .Build());
    }
}
