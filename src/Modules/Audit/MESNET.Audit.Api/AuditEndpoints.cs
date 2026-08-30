using MESNET.Audit.Application.Dtos;
using MESNET.Audit.Application.Queries;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Audit.Api;

public static class AuditEndpoints
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/audit")
            .WithTags("Audit").RequireAuthorization();

        // "Kendi işlemlerim" ek izin GEREKTİRMEZ: kullanıcının kendi geçmişini görmesi bir
        // yetki sorusu değildir. Kapsam sunucuda ActorId ile daraltılır; istemcinin
        // gönderdiği scope bir niyettir, yetki değil.
        group.MapGet("/mine", GetMine);

        // Kurum ağacı izi. Yol önekiyle daraltma handler'da; buradaki izin ERİŞİMİ açar,
        // kapsamı belirlemez.
        group.MapGet("/institution", GetForInstitution)
            .RequireAuthorization(Permissions.Audit.ViewInstitution);

        return app;
    }

    private static Task<IResult> GetMine(
        Guid? actorId, string? commandType, string? outcome,
        DateTimeOffset? from, DateTimeOffset? to,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = true,
        string? search = null, IMessageBus bus = default!)
        => Query(
            new GetAuditEntries(GetAuditEntries.ScopeMine, actorId, commandType, outcome, from, to),
            page, pageSize, sortBy, descending, search, bus);

    private static Task<IResult> GetForInstitution(
        Guid? actorId, string? commandType, string? outcome,
        DateTimeOffset? from, DateTimeOffset? to, bool? crossedTenantBoundary,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = true,
        string? search = null, IMessageBus bus = default!)
        => Query(
            new GetAuditEntries(
                GetAuditEntries.ScopeInstitution, actorId, commandType, outcome, from, to,
                crossedTenantBoundary),
            page, pageSize, sortBy, descending, search, bus);

    private static async Task<IResult> Query(
        GetAuditEntries query,
        int page, int pageSize, string? sortBy, bool descending, string? search,
        IMessageBus bus)
    {
        var result = await bus.InvokeAsync<PagedResult<AuditEntryDto>>(query with
        {
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            Descending = descending,
            Search = search,
        });

        return Results.Ok(ResponseBuilder.Success().AddData(result).Build());
    }
}
