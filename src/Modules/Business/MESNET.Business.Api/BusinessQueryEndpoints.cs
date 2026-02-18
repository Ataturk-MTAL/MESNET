using Marten;
using MESNET.Business.Application.Commands;
using MESNET.Business.Application.Dtos;
using MESNET.Business.Application.Errors;
using MESNET.Business.Application.Extensions;
using MESNET.Business.Core.Enums;
using MESNET.Business.Core.ValueObjects;
using MESNET.Business.Shared.Events;
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

    private static async Task<IResult> GetByStatus(
        string? status, IQuerySession session)
    {
        if (status is not null && BusinessStatus.TryFromName(status, true, out var businessStatus))
        {
            var filtered = await session.Query<Core.Entities.Business>()
                .Where(b => b.Status == businessStatus)
                .ToListAsync();
            return Results.Ok(ResponseBuilder.Success()
                .AddData(filtered.Select(b => b.ToDto()).ToList())
                .Build());
        }

        var all = await session.Query<Core.Entities.Business>()
            .ToListAsync();
        return Results.Ok(ResponseBuilder.Success()
            .AddData(all.Select(b => b.ToDto()).ToList())
            .Build());
    }

    private static async Task<IResult> GetNearby(
        double lat, double lng, double radius, IQuerySession session)
    {
        var allActive = await session.Query<Core.Entities.Business>()
            .Where(b => b.Status == BusinessStatus.Active && b.Location != null)
            .ToListAsync();

        var nearby = allActive
            .Where(b => CalculateDistanceKm(lat, lng,
                b.Location!.Latitude, b.Location.Longitude) <= radius)
            .Select(b => b.ToDto())
            .ToList();

        return Results.Ok(ResponseBuilder.Success()
            .AddData(nearby)
            .Build());
    }

    private static async Task<IResult> PutCapacity(
        Guid businessId, UpdateCapacity command, IDocumentSession session, IMessageBus bus)
    {
        var business = await session.LoadAsync<Core.Entities.Business>(businessId);
        if (business is null)
            return Results.NotFound(ResponseBuilder.Fail(404)
                .AddMessage(BusinessErrors.NotFound(businessId).Description)
                .AddErrors(BusinessErrors.NotFound(businessId))
                .Build());

        business.Capacity = new BusinessCapacity
        {
            TotalSlots = command.TotalSlots,
            OccupiedSlots = command.OccupiedSlots
        };

        session.Store(business);

        await bus.PublishAsync(
            new BusinessCapacityChanged(business.Id, business.Capacity.TotalSlots, business.Capacity.OccupiedSlots));

        return Results.Ok(ResponseBuilder.Success()
            .AddMessage("Kapasite güncellendi.")
            .Build());
    }

    private static double CalculateDistanceKm(
        double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusKm = 6371.0;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;
}
