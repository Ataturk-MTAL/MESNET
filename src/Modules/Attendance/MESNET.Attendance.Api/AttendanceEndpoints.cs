using Marten;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Application.Extensions;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Attendance.Api;

public static class AttendanceEndpoints
{
    public static void MapAttendanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/attendance").RequireAuthorization();

        group.MapPost("/", Post).RequireAuthorization(Permissions.Attendance.Manage);
        group.MapPost("/{attendanceId:guid}/verify", PostVerify).RequireAuthorization(Permissions.Attendance.Approve);
        group.MapPost("/{attendanceId:guid}/correct", PostCorrect).RequireAuthorization(Permissions.Attendance.Manage);
        group.MapPost("/{attendanceId:guid}/health-report", PostHealthReport).RequireAuthorization(Permissions.Attendance.Manage);
        group.MapGet("/{attendanceId:guid}", Get).RequireAuthorization(Permissions.Attendance.View);
        group.MapGet("/", GetAll).RequireAuthorization(Permissions.Attendance.View);
    }

    private static async Task<IResult> Post(
        MarkAttendance command, IMessageBus bus)
    {
        var (result, @event) = await bus.InvokeAsync<(Result, AttendanceMarked)>(command);

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Created(
            $"/api/attendance/{@event.AttendanceId}",
            ResponseBuilder.Success(201)
                .AddData(new { attendanceId = @event.AttendanceId })
                .AddMessage("Devamsızlık kaydı oluşturuldu.")
                .Build());
    }

    private static async Task<IResult> PostVerify(
        Guid attendanceId, VerifyAttendance command, IMessageBus bus)
    {
        var (result, @event) = await bus.InvokeAsync<(Result, AttendanceVerified)>(
            command with { AttendanceId = attendanceId });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Devamsızlık kaydı doğrulandı.")
            .Build());
    }

    private static async Task<IResult> PostCorrect(
        Guid attendanceId, CorrectAttendance command, IMessageBus bus)
    {
        var (result, @event) = await bus.InvokeAsync<(Result, AttendanceCorrected)>(
            command with { AttendanceId = attendanceId });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Devamsızlık kaydı düzeltildi.")
            .Build());
    }

    private static async Task<IResult> PostHealthReport(
        Guid attendanceId, AttachHealthReport command, IMessageBus bus)
    {
        var (result, @event) = await bus.InvokeAsync<(Result, HealthReportAttached)>(
            command with { AttendanceId = attendanceId });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sağlık raporu ilişkilendirildi.")
            .Build());
    }

    private static async Task<IResult> Get(
        Guid attendanceId, IQuerySession session)
    {
        var record = await session.Events.AggregateStreamAsync<AttendanceRecord>(attendanceId);
        if (record is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(AttendanceErrors.NotFound(attendanceId).Description)
                .AddErrors(AttendanceErrors.NotFound(attendanceId))
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(record.ToDto())
            .Build());
    }

    private static async Task<IResult> GetAll(
        Guid? studentId, Guid? businessId, Guid? institutionId, string? status,
        IQuerySession session)
    {
        IQueryable<AttendanceRecord> queryable = session.Query<AttendanceRecord>();

        if (studentId.HasValue)
            queryable = queryable.Where(r => r.StudentId == studentId.Value);

        if (businessId.HasValue)
            queryable = queryable.Where(r => r.BusinessId == businessId.Value);

        if (institutionId.HasValue)
            queryable = queryable.Where(r => r.InstitutionId == institutionId.Value);

        if (!string.IsNullOrWhiteSpace(status) &&
            AttendanceStatus.TryFromName(status, true, out var attendanceStatus))
            queryable = queryable.Where(r => r.Status == attendanceStatus);

        var records = await queryable.ToListAsync();
        return Results.Ok(ResponseBuilder.Success()
            .AddData(records.Select(r => r.ToDto()).ToList())
            .Build());
    }
}
