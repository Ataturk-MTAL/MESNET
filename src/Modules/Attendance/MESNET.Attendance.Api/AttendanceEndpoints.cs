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
        // Düzeltme devamsızlık TÜRÜNÜ değiştirir, yani doğrudan para sonucu doğurur: kaydı
        // "Mazeretsiz"den "Sağlık Raporu"na çevirmek kesintiyi kaldırır. Bu yüzden uç
        // "attendance:manage" değil "attendance:direct-entry" ister (#172) — o izin yalnız okul
        // rollerindedir. Önceden işletme yetkilisi bu uçtan onay zincirini tümden atlayarak
        // türü değiştirebiliyordu.
        group.MapPost("/{attendanceId:guid}/correct", PostCorrect).RequireAuthorization(Permissions.Attendance.DirectEntry);

        // Sağlık raporu girişi bilinçli olarak GENİŞTİR (#172): işletme yetkilisi, işletme İK,
        // usta öğretici ve öğrenci de yükleyebilir. Hüküm doğurup doğurmadığına handler karar
        // verir; yükleyende "attendance:health-report:direct" yoksa rapor onaya düşer.
        group.MapPost("/{attendanceId:guid}/health-report", PostHealthReport)
            .RequireAuthorization(Permissions.Attendance.Upload)
            .DisableAntiforgery();

        // Onay zincirinin 1. adımı — koordinatör öğretmen (müdür yardımcısı ve müdürde de var).
        group.MapPost("/{attendanceId:guid}/health-report/approve", PostApproveHealthReport)
            .RequireAuthorization(Permissions.Attendance.Approve);
        group.MapPost("/{attendanceId:guid}/health-report/reject", PostRejectHealthReport)
            .RequireAuthorization(Permissions.Attendance.Approve);
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

    // ────────────────────────────────────────────────────────────────────────────────
    // POST /api/attendance/{attendanceId}/health-report — Sağlık raporu yükle (#172)
    // Form alanı: ReportFile (IFormFile) — PDF, JPEG veya PNG
    // ────────────────────────────────────────────────────────────────────────────────
    private static async Task<IResult> PostHealthReport(
        Guid attendanceId, HttpRequest request, IMessageBus bus)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("Multipart form-data bekleniyor.")
                .Build());

        // Bozuk ya da boş multipart gövde ReadFormAsync'te istisna atar (parçasız gövdede
        // "Unexpected end of Stream"). Bu bir istemci hatasıdır — 500 değil 400 dönmelidir.
        IFormCollection form;
        try
        {
            form = await request.ReadFormAsync();
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException)
        {
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("Multipart form-data gövdesi okunamadı.")
                .Build());
        }

        var reportFile = form.Files["ReportFile"] ?? form.Files.FirstOrDefault();

        if (reportFile is null)
            return Results.BadRequest(ResponseBuilder.Fail()
                .AddMessage("Sağlık raporu dosyası (ReportFile) eksik.")
                .Build());

        await bus.InvokeAsync(new AttachHealthReport(attendanceId, reportFile));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sağlık raporu yüklendi.")
            .Build());
    }

    private static async Task<IResult> PostApproveHealthReport(
        Guid attendanceId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ApproveHealthReport(attendanceId));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sağlık raporu onaylandı.")
            .Build());
    }

    private static async Task<IResult> PostRejectHealthReport(
        Guid attendanceId, RejectHealthReport command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { AttendanceId = attendanceId });

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Sağlık raporu reddedildi.")
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
        int? year, int? month, string? branchCode = null,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = false, string? search = null,
        IMessageBus bus = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<AttendanceRecordDto>>(
            new ListAttendanceRecords(studentId, businessId, institutionId, academicPeriodId, status, year, month, branchCode)
            { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending, Search = search });

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
            .Build());
    }
}
