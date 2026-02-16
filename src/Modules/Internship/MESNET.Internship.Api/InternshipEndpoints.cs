using Marten;
using MESNET.Common.Shared;
using MESNET.Internship.Application.Commands;
using MESNET.Internship.Application.Errors;
using MESNET.Internship.Application.Extensions;
using MESNET.Internship.Core.Entities;
using MESNET.Internship.Core.Enums;
using MESNET.Internship.Shared.Events;
using Microsoft.AspNetCore.Http;
using Wolverine;
using Wolverine.Http;

namespace MESNET.Internship.Api;

public static class InternshipEndpoints
{
    [WolverineGet("/api/internships/{internshipId}")]
    public static async Task<IResult> Get(
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

    [WolverineGet("/api/internships")]
    public static async Task<IResult> GetAll(
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

    [WolverinePost("/api/internships/{internshipId}/terminate")]
    public static async Task<IResult> PostRequestTermination(
        Guid internshipId, RequestTermination command, IMessageBus bus)
    {
        var (result, @event) = await bus.InvokeAsync<(Result, InternshipTerminationRequested)>(
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

    [WolverinePost("/api/internships/{internshipId}/approve/parent")]
    public static async Task<IResult> PostApproveParent(
        Guid internshipId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ApproveTerminationByParent(internshipId));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Veli onayı verildi.")
            .Build());
    }

    [WolverinePost("/api/internships/{internshipId}/approve/teacher")]
    public static async Task<IResult> PostApproveTeacher(
        Guid internshipId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ApproveTerminationByTeacher(internshipId));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Koordinatör öğretmen onayı verildi.")
            .Build());
    }

    [WolverinePost("/api/internships/{internshipId}/approve/deputy")]
    public static async Task<IResult> PostApproveDeputy(
        Guid internshipId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ApproveTerminationByDeputy(internshipId));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Müdür yardımcısı onayı verildi.")
            .Build());
    }

    [WolverinePost("/api/internships/{internshipId}/approve/director")]
    public static async Task<IResult> PostApproveDirector(
        Guid internshipId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ApproveTerminationByDirector(internshipId));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Müdür onayı verildi.")
            .Build());
    }

    [WolverinePost("/api/internships/{internshipId}/approve/business")]
    public static async Task<IResult> PostApproveBusinessRep(
        Guid internshipId, IMessageBus bus)
    {
        await bus.InvokeAsync(new ApproveTerminationByBusinessRep(internshipId));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("İşletme yetkilisi onayı verildi.")
            .Build());
    }

    [WolverinePost("/api/internships/{internshipId}/approve/override")]
    public static async Task<IResult> PostOverride(
        Guid internshipId, OverrideTerminationApproval command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { InternshipId = internshipId });
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Onay zinciri override edildi.")
            .Build());
    }
}
