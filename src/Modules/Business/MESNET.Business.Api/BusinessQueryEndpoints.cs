using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Dtos;
using MESNET.Business.Application.Queries;
using MESNET.Common.Shared;
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
        group.MapGet("/nearby", GetNearby).RequireAuthorization(Permissions.Company.View);
        group.MapPut("/{businessId:guid}/capacity", PutCapacity).RequireAuthorization(Permissions.Company.Manage);
        return app;
    }

    private static async Task<IResult> GetByStatus(string? status, IMessageBus bus)
    {
        var businesses = await bus.InvokeAsync<IReadOnlyList<BusinessDto>>(new GetBusinessesByStatus(status));
        return Results.Ok(ResponseBuilder.Success().AddData(businesses).Build());
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
