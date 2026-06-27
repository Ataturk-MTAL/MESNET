using MESNET.Common.Shared;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using MESNET.Enrollment.Application.Commands;
using MESNET.Enrollment.Application.Dtos;
using MESNET.Enrollment.Application.Handlers;
using MESNET.Enrollment.Application.Queries;
using MESNET.Enrollment.Shared.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Enrollment.Api;

public sealed record DeregisterStudentRequest(string Reason);

public static class StudentEndpoints
{
    public static IEndpointRouteBuilder MapStudentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/students").RequireAuthorization();

        group.MapPost("/", Post).RequireAuthorization(Permissions.Student.Manage);
        group.MapPatch("/{studentId:guid}", Patch).RequireAuthorization(Permissions.Student.Manage);
        group.MapPost("/{studentId:guid}/deregister", PostDeregister).RequireAuthorization(Permissions.Student.Manage);
        group.MapPost("/sync-counts", PostSyncCounts).RequireAuthorization(Permissions.Student.Manage);
        group.MapGet("/{studentId:guid}", Get).RequireAuthorization(Permissions.Student.View);
        group.MapGet("/", GetAll).RequireAuthorization(Permissions.Student.View);

        return app;
    }

    private static async Task<IResult> Post(RegisterStudent command, IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<StudentProfileDto>(command);
        return Results.Created($"/api/students/{dto.Id}",
            ResponseBuilder.Success(201)
                .AddData(dto)
                .AddMessage("Öğrenci kaydedildi.")
                .Build());
    }

    private static async Task<IResult> PostDeregister(Guid studentId, DeregisterStudentRequest request, IMessageBus bus)
    {
        await bus.InvokeAsync(new DeregisterStudent(studentId, request.Reason));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Öğrenci kaydı silindi.")
            .Build());
    }

    private static async Task<IResult> PostSyncCounts(SyncStudentCounts command, IMessageBus bus)
    {
        // Event yayını handler içinde yapılır (StudentCountsSynced cross-module → PublishAsync).
        // Endpoint yalnız komutu çalıştırıp özet sonucu döner.
        var result = await bus.InvokeAsync<SyncStudentCountsResult>(command);

        // Sayıları doğrudan response'ta dön — async event'ler henüz işlenmemiş olabilir
        var counts = result.Events.ToDictionary(
            e => $"{e.BranchCode}:{e.EducationType}",
            e => e.CountsByClassYear);

        return Results.Ok(ResponseBuilder.Success()
            .AddData(new { syncedBranches = result.Events.Count, counts })
            .AddMessage($"{result.Events.Count} alan için öğrenci sayıları senkronize edildi.")
            .Build());
    }

    private static async Task<IResult> Patch(Guid studentId, UpdateStudentProfile command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { StudentId = studentId });
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Öğrenci profili güncellendi.")
            .Build());
    }

    private static async Task<IResult> Get(Guid studentId, IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<StudentProfileDto?>(new GetStudentProfile(studentId));
        if (dto is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage($"Öğrenci bulunamadı: {studentId}").Build());

        return Results.Ok(ResponseBuilder.Success().AddData(dto).Build());
    }

    private static async Task<IResult> GetAll(
        Guid? institutionId, Guid? academicPeriodId, string? branchCode, string? section, string? status,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = false, string? search = null,
        IMessageBus bus = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<StudentProfileDto>>(
            new ListStudents(institutionId, academicPeriodId, branchCode, section, status)
            { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending, Search = search });
        return Results.Ok(ResponseBuilder.Success().AddData(result).Build());
    }
}
