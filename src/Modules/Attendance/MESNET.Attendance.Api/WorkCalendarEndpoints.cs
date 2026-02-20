using Marten;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Application.Extensions;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Attendance.Api;

public static class WorkCalendarEndpoints
{
    public static void MapWorkCalendarEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/work-calendar").RequireAuthorization();

        group.MapPost("/", Post).RequireAuthorization(Permissions.Institution.Manage);
        group.MapGet("/", Get).RequireAuthorization(Permissions.Attendance.View);
    }

    private static async Task<IResult> Post(
        UpdateWorkCalendar command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result<Guid>>(command);

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { calendarId = result.Value })
            .AddMessage("Çalışma takvimi güncellendi.")
            .Build());
    }

    private static async Task<IResult> Get(
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
