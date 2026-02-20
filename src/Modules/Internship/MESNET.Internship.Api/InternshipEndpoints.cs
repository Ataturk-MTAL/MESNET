using Marten;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Internship.Application.Commands;
using MESNET.Internship.Application.Errors;
using MESNET.Internship.Application.Extensions;
using MESNET.Internship.Core.Entities;
using MESNET.Internship.Core.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Internship.Api;

public static class InternshipEndpoints
{
    public static void MapInternshipEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/internships").WithTags("Internship").RequireAuthorization();

        group.MapGet("/{internshipId:guid}", Get).RequireAuthorization(Permissions.Internship.View);
        group.MapGet("/", GetAll).RequireAuthorization(Permissions.Internship.View);
        group.MapPost("/{internshipId:guid}/terminate", PostRequestTermination).RequireAuthorization(Permissions.Internship.Manage);
        group.MapPost("/{internshipId:guid}/approve/parent", PostApproveParent).RequireAuthorization(Permissions.Internship.Approve);
        group.MapPost("/{internshipId:guid}/approve/teacher", PostApproveTeacher).RequireAuthorization(Permissions.Internship.Approve);
        group.MapPost("/{internshipId:guid}/approve/deputy", PostApproveDeputy).RequireAuthorization(Permissions.Internship.Approve);
        group.MapPost("/{internshipId:guid}/approve/director", PostApproveDirector).RequireAuthorization(Permissions.Internship.Manage);
        group.MapPost("/{internshipId:guid}/approve/business", PostApproveBusinessRep).RequireAuthorization(Permissions.Company.Student);
        group.MapPost("/{internshipId:guid}/approve/override", PostOverride).RequireAuthorization(Permissions.Internship.Manage);
    }

    private static async Task<IResult> Get(
        Guid internshipId, IQuerySession session)
    {
        var summary = await session.LoadAsync<InternshipSummary>(internshipId);
        if (summary is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(InternshipErrors.NotFound(internshipId).Description)
                .AddErrors(InternshipErrors.NotFound(internshipId))
                .Build());

        return Results.Ok(ResponseBuilder.Success()
            .AddData(summary.ToDto())
            .Build());
    }

    private static async Task<IResult> GetAll(
        Guid? studentId, Guid? businessId, Guid? institutionId, string? phase,
        IQuerySession session)
    {
        IQueryable<InternshipSummary> queryable = session.Query<InternshipSummary>();

        if (studentId.HasValue)
            queryable = queryable.Where(s => s.StudentId == studentId.Value);

        if (businessId.HasValue)
            queryable = queryable.Where(s => s.BusinessId == businessId.Value);

        if (institutionId.HasValue)
            queryable = queryable.Where(s => s.InstitutionId == institutionId.Value);

        if (!string.IsNullOrWhiteSpace(phase) &&
            InternshipPhase.TryFromName(phase, true, out var internshipPhase))
            queryable = queryable.Where(s => s.Phase == internshipPhase);

        var summaries = await queryable.ToListAsync();
        return Results.Ok(ResponseBuilder.Success()
            .AddData(summaries.Select(s => s.ToDto()).ToList())
            .Build());
    }

    private static async Task<IResult> PostRequestTermination(
        Guid internshipId, RequestTermination command, IMessageBus bus)
    {
        var result = await bus.InvokeAsync<Result>(
            command with { InternshipId = internshipId });

        if (result.IsFailure)
        {
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(result.Error.Description)
                .AddErrors(result.Error)
                .Build());
        }

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Fesih talebi oluşturuldu.")
            .Build());
    }

    private static async Task<IResult> PostApproveParent(
        Guid internshipId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ApproveTerminationByParent(internshipId));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Veli onayı verildi.")
            .Build());
    }

    private static async Task<IResult> PostApproveTeacher(
        Guid internshipId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ApproveTerminationByTeacher(internshipId));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Koordinatör öğretmen onayı verildi.")
            .Build());
    }

    private static async Task<IResult> PostApproveDeputy(
        Guid internshipId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ApproveTerminationByDeputy(internshipId));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Müdür yardımcısı onayı verildi.")
            .Build());
    }

    private static async Task<IResult> PostApproveDirector(
        Guid internshipId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ApproveTerminationByDirector(internshipId));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Müdür onayı verildi.")
            .Build());
    }

    private static async Task<IResult> PostApproveBusinessRep(
        Guid internshipId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ApproveTerminationByBusinessRep(internshipId));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme yetkilisi onayı verildi.")
            .Build());
    }

    private static async Task<IResult> PostOverride(
        Guid internshipId, OverrideTerminationApproval command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { InternshipId = internshipId });
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Onay zinciri override edildi.")
            .Build());
    }
}
