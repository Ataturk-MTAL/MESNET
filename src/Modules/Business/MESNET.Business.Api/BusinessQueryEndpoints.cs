using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Dtos;
using MESNET.Business.Application.Queries;
using MESNET.Business.Core.Enums;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wolverine;

namespace MESNET.Business.Api;

public static class BusinessQueryEndpoints
{
    public static IEndpointRouteBuilder MapBusinessQueryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/businesses").WithTags("BusinessQuery").RequireAuthorization();
        group.MapGet("/", GetByStatus).RequireAuthorization(Permissions.Company.View);
        group.MapGet("/sectors", GetSectors).RequireAuthorization(Permissions.Company.View);
        group.MapGet("/nearby", GetNearby).RequireAuthorization(Permissions.Company.View);
        group.MapPut("/{businessId:guid}/capacity", PutCapacity).RequireAuthorization(Permissions.Company.Manage);
        return app;
    }

    private static async Task<IResult> GetByStatus(
        string? status, string? sector, string? branchCode,
        int page = 1, int pageSize = 20, string? sortBy = null, bool descending = false, string? search = null,
        IMessageBus bus = default!)
    {
        var result = await bus.InvokeAsync<PagedResult<BusinessDto>>(
            new GetBusinessesByStatus(status, sector, branchCode)
            {
                Page = page, PageSize = pageSize,
                SortBy = sortBy, Descending = descending, Search = search,
            });
        return Results.Ok(ResponseBuilder.Success().AddData(result).Build());
    }

    private static IResult GetSectors()
    {
        var sectors = BusinessSector.List
            .OrderBy(s => s.Value)
            .Select(s => new SectorDto(s.Name, s.Slug))
            .ToList();
        return Results.Ok(ResponseBuilder.Success().AddData(sectors).Build());
    }

    private static async Task<IResult> GetNearby(double lat, double lng, double radius, IMessageBus bus)
    {
        var businesses = await bus.InvokeAsync<IReadOnlyList<BusinessDto>>(
            new SearchNearbyBusinesses(lat, lng, radius));
        return Results.Ok(ResponseBuilder.Success().AddData(businesses).Build());
    }

    private static async Task<IResult> PutCapacity(Guid businessId, UpdateCapacity command, IMessageBus bus)
    {
        await bus.InvokeAsync(command with { BusinessId = businessId });
        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kapasite güncellendi.")
            .Build());
    }
}
