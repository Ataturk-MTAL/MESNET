using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using MESNET.Internship.Application.Commands;
using MESNET.Internship.Application.Dtos;
using MESNET.Internship.Application.Queries;
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
        // Veli adımı ayrı izin ister (#174): "internship:approve" verilseydi veli
        // /approve/teacher ve /approve/deputy uçlarına da erişir, zincirin üç adımını tek
        // başına tamamlardı. Okul tarafı bu izni de taşır — bugünkü davranış korunur.
        group.MapPost("/{internshipId:guid}/approve/parent", PostApproveParent).RequireAuthorization(Permissions.Internship.ApproveParent);
        group.MapPost("/{internshipId:guid}/approve/teacher", PostApproveTeacher).RequireAuthorization(Permissions.Internship.Approve);
        group.MapPost("/{internshipId:guid}/approve/deputy", PostApproveDeputy).RequireAuthorization(Permissions.Internship.Approve);
        group.MapPost("/{internshipId:guid}/approve/director", PostApproveDirector).RequireAuthorization(Permissions.Internship.Manage);
        group.MapPost("/{internshipId:guid}/approve/business", PostApproveBusinessRep).RequireAuthorization(Permissions.Company.Student);
        group.MapPost("/{internshipId:guid}/approve/override", PostOverride).RequireAuthorization(Permissions.Internship.Manage);
    }

    private static async Task<IResult> Get(
        Guid internshipId, IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<InternshipSummaryDto>(new GetInternship(internshipId));

        return Results.Ok(ResponseBuilder.Success()
            .AddData(dto)
            .Build());
    }

    private static async Task<IResult> GetAll(
        Guid? studentId, Guid? businessId, Guid? institutionId, Guid? academicPeriodId,
        string? phase, int? minAbsenceDays,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = false, string? search = null,
        ICurrentUserService currentUser = default!, IMessageBus bus = default!)
    {
        var user = currentUser.GetCurrentUser();
        var effectiveInstitutionId = institutionId ?? user?.InstitutionId;

        var result = await bus.InvokeAsync<PagedResult<InternshipSummaryDto>>(
            new ListInternships(studentId, businessId, effectiveInstitutionId, academicPeriodId, phase, minAbsenceDays)
            { Page = page, PageSize = pageSize, SortBy = sortBy, Descending = descending, Search = search });

        return Results.Ok(ResponseBuilder.Success()
            .AddData(result)
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
