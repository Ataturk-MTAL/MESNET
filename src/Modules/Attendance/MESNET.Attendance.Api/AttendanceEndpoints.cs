using Marten;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Application.Extensions;
using MESNET.Attendance.Core.Aggregates;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Shared;
using Microsoft.AspNetCore.Http;
using Wolverine;
using Wolverine.Http;

namespace MESNET.Attendance.Api;

public static class AttendanceEndpoints
{
    [WolverinePost("/api/attendance")]
    public static async Task<IResult> Post(
        MarkAttendance command, IMessageBus bus)
    {
        try
        {
            var @event = await bus.InvokeAsync<AttendanceMarked>(command);
            return Results.Created(
                $"/api/attendance/{@event.AttendanceId}",
                ResponseBuilder.Success(201)
                    .AddData(new { attendanceId = @event.AttendanceId })
                    .AddMessage("Devamsızlık kaydı oluşturuldu.")
                    .Build());
        }
        catch (InvalidOperationException ex)
        {
            var error = AttendanceErrors.OperationFailed("Mark", ex.Message);
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(error.Description)
                .AddErrors(error)
                .Build());
        }
    }

    [WolverinePost("/api/attendance/{attendanceId}/verify")]
    public static async Task<IResult> PostVerify(
        Guid attendanceId, VerifyAttendance command, IMessageBus bus)
    {
        try
        {
            await bus.InvokeAsync(command with { AttendanceId = attendanceId });
            return Results.Ok(ResponseBuilder.Success()
                .AddMessage("Devamsızlık kaydı doğrulandı.")
                .Build());
        }
        catch (InvalidOperationException ex)
        {
            var error = AttendanceErrors.OperationFailed("Verify", ex.Message);
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(error.Description)
                .AddErrors(error)
                .Build());
        }
    }

    [WolverinePost("/api/attendance/{attendanceId}/correct")]
    public static async Task<IResult> PostCorrect(
        Guid attendanceId, CorrectAttendance command, IMessageBus bus)
    {
        try
        {
            await bus.InvokeAsync(command with { AttendanceId = attendanceId });
            return Results.Ok(ResponseBuilder.Success()
                .AddMessage("Devamsızlık kaydı düzeltildi.")
                .Build());
        }
        catch (InvalidOperationException ex)
        {
            var error = AttendanceErrors.OperationFailed("Correct", ex.Message);
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(error.Description)
                .AddErrors(error)
                .Build());
        }
    }

    [WolverinePost("/api/attendance/{attendanceId}/health-report")]
    public static async Task<IResult> PostHealthReport(
        Guid attendanceId, AttachHealthReport command, IMessageBus bus)
    {
        try
        {
            await bus.InvokeAsync(command with { AttendanceId = attendanceId });
            return Results.Ok(ResponseBuilder.Success()
                .AddMessage("Sağlık raporu ilişkilendirildi.")
                .Build());
        }
        catch (InvalidOperationException ex)
        {
            var error = AttendanceErrors.OperationFailed("AttachHealthReport", ex.Message);
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(error.Description)
                .AddErrors(error)
                .Build());
        }
    }

    [WolverineGet("/api/attendance/{attendanceId}")]
    public static async Task<IResult> Get(
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

    [WolverineGet("/api/attendance")]
    public static async Task<IResult> GetAll(
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
