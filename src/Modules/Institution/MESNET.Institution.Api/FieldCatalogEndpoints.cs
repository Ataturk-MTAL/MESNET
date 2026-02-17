using Marten;
using MESNET.Common.Shared;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Errors;
using MESNET.Institution.Application.Extensions;
using MESNET.Institution.Core.Entities;
using MESNET.Institution.Core.Enums;
using MESNET.Institution.Core.ValueObjects;
using MESNET.Institution.Shared.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Institution.Api;

public static class FieldCatalogEndpoints
{
    public static IEndpointRouteBuilder MapFieldCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/field-catalog", GetFieldCatalog)
            .WithTags("FieldCatalog");

        var branches = app.MapGroup("/api/institutions/{institutionId:guid}/branches")
            .WithTags("FieldCatalog");

        branches.MapPost("/", PostBranch);
        branches.MapDelete("/{fieldCode}", DeleteBranch);
        branches.MapPut("/{fieldCode}/specializations", PutSpecializations);

        return app;
    }

    private static async Task<IResult> GetFieldCatalog(
        string? educationType, IQuerySession session)
    {
        if (educationType is not null && EducationType.TryFromName(educationType, true, out var type))
        {
            var filtered = await session.Query<FieldOfStudy>()
                .Where(f => f.Type == type && f.IsActive)
                .ToListAsync();
            return Results.Ok(ResponseBuilder.Success()
                .AddData(filtered.Select(f => f.ToDto()).ToList())
                .Build());
        }

        var all = await session.Query<FieldOfStudy>()
            .Where(f => f.IsActive)
            .ToListAsync();
        return Results.Ok(ResponseBuilder.Success()
            .AddData(all.Select(f => f.ToDto()).ToList())
            .Build());
    }

    private static async Task<IResult> PostBranch(
        Guid institutionId, ActivateBranch command, IDocumentSession session, IMessageBus bus)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(institutionId);
        if (institution is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(InstitutionErrors.NotFound(institutionId).Description)
                .AddErrors(InstitutionErrors.NotFound(institutionId))
                .Build());

        if (institution.Branches.Any(b => b.FieldCode == command.FieldCode && b.IsActive))
            return Results.BadRequest(ResponseBuilder.Fail(400)
                .AddMessage(InstitutionErrors.BranchAlreadyActive(command.FieldCode).Description)
                .AddErrors(InstitutionErrors.BranchAlreadyActive(command.FieldCode))
                .Build());

        var field = await session.Query<FieldOfStudy>()
            .FirstOrDefaultAsync(f => f.Code == command.FieldCode);
        if (field is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(InstitutionErrors.FieldOfStudyNotFound(command.FieldCode).Description)
                .AddErrors(InstitutionErrors.FieldOfStudyNotFound(command.FieldCode))
                .Build());

        var branch = new InstitutionBranch
        {
            FieldCode = field.Code,
            FieldName = field.Name,
            Type = field.Type
        };

        institution.Branches.Add(branch);
        session.Store(institution);

        await bus.PublishAsync(
            new BranchActivated(institution.Id, field.Code, field.Name, field.Type));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Alan aktifleştirildi.")
            .Build());
    }

    private static async Task<IResult> DeleteBranch(
        Guid institutionId, string fieldCode, IDocumentSession session, IMessageBus bus)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(institutionId);
        if (institution is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(InstitutionErrors.NotFound(institutionId).Description)
                .AddErrors(InstitutionErrors.NotFound(institutionId))
                .Build());

        var branch = institution.Branches.FirstOrDefault(b => b.FieldCode == fieldCode && b.IsActive);
        if (branch is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(InstitutionErrors.BranchNotFound(fieldCode).Description)
                .AddErrors(InstitutionErrors.BranchNotFound(fieldCode))
                .Build());

        branch.IsActive = false;
        session.Store(institution);

        await bus.PublishAsync(
            new BranchDeactivated(institution.Id, fieldCode));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Alan pasife alındı.")
            .Build());
    }

    private static async Task<IResult> PutSpecializations(
        Guid institutionId, string fieldCode,
        UpdateBranchSpecializations command, IDocumentSession session, IMessageBus bus)
    {
        var institution = await session.LoadAsync<Core.Entities.Institution>(institutionId);
        if (institution is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(InstitutionErrors.NotFound(institutionId).Description)
                .AddErrors(InstitutionErrors.NotFound(institutionId))
                .Build());

        var branch = institution.Branches.FirstOrDefault(b => b.FieldCode == fieldCode && b.IsActive);
        if (branch is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(InstitutionErrors.BranchNotFound(fieldCode).Description)
                .AddErrors(InstitutionErrors.BranchNotFound(fieldCode))
                .Build());

        var updated = branch with { ActiveSpecializations = command.ActiveSpecializations };
        var index = institution.Branches.IndexOf(branch);
        institution.Branches[index] = updated;

        session.Store(institution);

        await bus.PublishAsync(
            new BranchSpecializationsUpdated(institution.Id, fieldCode, command.ActiveSpecializations));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Uzmanlık alanları güncellendi.")
            .Build());
    }
}
