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

        // Liste hem okul tarafına hem veri sahibine açıktır (#182).
        group.MapGet("/{internshipId:guid}", Get).RequireAuthorization(PermissionPolicies.InternshipViewOrOwn);
        group.MapGet("/", GetAll).RequireAuthorization(PermissionPolicies.InternshipViewOrOwn);
        // Zincir durumu okuma (#191) — daha önce hiçbir uçtan okunamıyordu.
        // Veri sahibine de açık: veli/öğrenci kendi sürecini görebilmeli. Kapsam handler'da.
        group.MapGet("/{internshipId:guid}/termination-chain", GetTerminationChainStatus)
            .RequireAuthorization(PermissionPolicies.InternshipViewOrOwn);
        group.MapPost("/{internshipId:guid}/terminate", PostRequestTermination).RequireAuthorization(Permissions.Internship.Manage);
        group.MapPost("/{internshipId:guid}/approve/teacher", PostApproveTeacher).RequireAuthorization(Permissions.Internship.Approve);
        group.MapPost("/{internshipId:guid}/approve/deputy", PostApproveDeputy).RequireAuthorization(Permissions.Internship.Approve);
        group.MapPost("/{internshipId:guid}/approve/director", PostApproveDirector).RequireAuthorization(Permissions.Internship.Manage);
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

    private static async Task<IResult> GetTerminationChainStatus(
        Guid internshipId, IMessageBus bus)
    {
        var dto = await bus.InvokeAsync<TerminationChainStatusDto>(
            new GetTerminationChain(internshipId));

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

    /// <summary>
    /// Fesih talebi açar. <b>Talebi kimin açtığı token'dan damgalanır</b> (#191): gövdede
    /// aktör alanı yoktur, dolayısıyla istemci başka birinin adına talep açtığını
    /// kaydettiremez.
    /// </summary>
    private static async Task<IResult> PostRequestTermination(
        Guid internshipId, RequestTerminationRequest request,
        ICurrentUserService currentUser, IMessageBus bus)
    {
        // InvokeAsync<Result> DEĞİL: handler InternshipTerminationRequested döndürüyor ve
        // Wolverine özel Result sarmalayıcısını anlamıyor — istek 500 dönüyordu. Fesih fiilen
        // açılıyor, yalnız yanıt patlıyordu; uç arayüzden hiç çağrılmadığı için görülmemişti.
        // Hata bildirimi DomainException ile gelir (422), Result ile değil.
        await bus.InvokeAsync(new RequestTermination(
            internshipId, request.Reason, request.ReasonType, currentUser.GetFullName()));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Fesih talebi oluşturuldu.")
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


    /// <summary>
    /// Onay zincirini atlar. <b>Kimin atladığı token'dan damgalanır</b> (#191) — override,
    /// zinciri tümüyle geçersizleştiren tek işlemdir; denetim izi istemciden gelemez.
    /// </summary>
    private static async Task<IResult> PostOverride(
        Guid internshipId, OverrideTerminationApprovalRequest request,
        ICurrentUserService currentUser, IMessageBus bus)
    {
        await bus.InvokeAsync(new OverrideTerminationApproval(
            internshipId, currentUser.GetFullName(), request.Reason));
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Onay zinciri override edildi.")
            .Build());
    }
}
