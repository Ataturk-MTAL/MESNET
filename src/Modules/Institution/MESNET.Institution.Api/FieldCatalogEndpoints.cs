using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Institution.Application.Commands;
using MESNET.Institution.Application.Dtos;
using MESNET.Institution.Application.Queries;
using MESNET.Institution.Core.Enums;
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
            .WithTags("FieldCatalog").RequireAuthorization(Permissions.Institution.View);

        var branches = app.MapGroup("/api/institutions/{institutionId:guid}/branches")
            .WithTags("FieldCatalog").RequireAuthorization();

        branches.MapPost("/", PostBranch).RequireAuthorization(Permissions.Institution.Manage);
        branches.MapDelete("/{fieldCode}", DeleteBranch).RequireAuthorization(Permissions.Institution.Manage);
        branches.MapPut("/{fieldCode}/specializations", PutSpecializations).RequireAuthorization(Permissions.Institution.Manage);
        branches.MapPut("/{fieldCode}/supervisors", PutSupervisors).RequireAuthorization(Permissions.Institution.Manage);

        return app;
    }

    private static async Task<IResult> GetFieldCatalog(string? educationType, IMessageBus bus)
    {
        EducationType? type = null;
        if (educationType is not null)
            EducationType.TryFromName(educationType, true, out type);

        var dtos = await bus.InvokeAsync<List<FieldOfStudyDto>>(new GetFieldCatalog(type));
        return Results.Ok(ResponseBuilder.Success()
            .AddData(dtos)
            .Build());
    }

    private static async Task<IResult> PostBranch(Guid institutionId, ActivateBranch command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { InstitutionId = institutionId });
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Alan aktifleştirildi.")
            .Build());
    }

    private static async Task<IResult> DeleteBranch(Guid institutionId, string fieldCode, IMessageBus bus)
    {
        await bus.InvokeAsync(new DeactivateBranch(institutionId, fieldCode));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Alan pasife alındı.")
            .Build());
    }

    private static async Task<IResult> PutSupervisors(
        Guid institutionId, string fieldCode,
        UpdateBranchSupervisorConfig command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { InstitutionId = institutionId, FieldCode = fieldCode });
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Şeflik yapılandırması güncellendi.")
            .Build());
    }

    private static async Task<IResult> PutSpecializations(
        Guid institutionId, string fieldCode,
        UpdateBranchSpecializations command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { InstitutionId = institutionId, FieldCode = fieldCode });
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Uzmanlık alanları güncellendi.")
            .Build());
    }
}
