using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Dtos;
using MESNET.Attendance.Application.Queries;
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
        var calendarId = await bus.InvokeAsync<Guid>(command);

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { calendarId })
            .AddMessage("Çalışma takvimi güncellendi.")
            .Build());
    }

    private static async Task<IResult> Get(
        Guid institutionId, int year, IMessageBus bus)
    {
        var calendar = await bus.InvokeAsync<WorkCalendarDto>(new GetWorkCalendar(institutionId, year));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(calendar)
            .Build());
    }
}
