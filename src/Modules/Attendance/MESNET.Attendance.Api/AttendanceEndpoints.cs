using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Dtos;
using MESNET.Attendance.Application.Queries;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Pagination;
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
        group.MapPost("/{attendanceId:guid}/approve", PostApprove).RequireAuthorization(Permissions.Attendance.Approve);
        group.MapPost("/{attendanceId:guid}/verify", PostVerify).RequireAuthorization(Permissions.Attendance.Approve);
        group.MapPost("/{attendanceId:guid}/correct", PostCorrect).RequireAuthorization(Permissions.Attendance.Manage);
        group.MapPost("/{attendanceId:guid}/health-report", PostHealthReport).RequireAuthorization(Permissions.Attendance.Manage);
        group.MapDelete("/{attendanceId:guid}", Delete).RequireAuthorization(Permissions.Attendance.Delete);
        group.MapGet("/{attendanceId:guid}", Get).RequireAuthorization(Permissions.Attendance.View);
        group.MapGet("/", GetAll).RequireAuthorization(Permissions.Attendance.View);
    }

    private static async Task<IResult> Post(
        MarkAttendance command, IMessageBus bus)
    {
        var attendanceId = await bus.InvokeAsync<Guid>(command);

        return Results.Created(
            $"/api/attendance/{attendanceId}",
            ResponseBuilder.Success(201)
                .AddData(new { attendanceId })
                .AddMessage("Devamsızlık kaydı oluşturuldu.")
                .Build());
    }

    private static async Task<IResult> PostApprove(
        Guid attendanceId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ApproveAttendance(attendanceId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Devamsızlık kaydı onaylandı.")
            .Build());
    }

    private static async Task<IResult> PostVerify(
        Guid attendanceId, IMessageBus bus)
    {
        await bus.InvokeAsync(new VerifyAttendance(attendanceId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Devamsızlık kaydı doğrulandı.")
            .Build());
    }

    private static async Task<IResult> PostCorrect(
        Guid attendanceId, CorrectAttendance command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { AttendanceId = attendanceId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Devamsızlık kaydı düzeltildi.")
            .Build());
    }

    private static async Task<IResult> PostHealthReport(
        Guid attendanceId, AttachHealthReport command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { AttendanceId = attendanceId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sağlık raporu ilişkilendirildi.")
            .Build());
    }

    private static async Task<IResult> Delete(
        Guid attendanceId, IMessageBus bus)
    {
        await bus.InvokeAsync(new DeleteAttendance(attendanceId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Devamsızlık kaydı silindi.")
            .Build());
    }

    private static async Task<IResult> Get(
        Guid attendanceId, IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<AttendanceRecordDto>(new GetAttendanceRecord(attendanceId));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(dto)
            .Build());
    }

    private static async Task<IResult> GetAll(
        Guid? studentId, Guid? businessId, Guid? institutionId, Guid? academicPeriodId, string? status,
        int? year, int? month,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = false,
        IMessageBus bus = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<AttendanceRecordDto>>(
            new ListAttendanceRecords(studentId, businessId, institutionId, academicPeriodId, status, year, month)
            { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending });

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .Build());
    }
}
