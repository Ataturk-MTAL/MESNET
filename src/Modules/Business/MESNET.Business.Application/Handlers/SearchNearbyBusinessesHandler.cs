using Marten;
using MESNET.Business.Application.Dtos;
using MESNET.Business.Application.Extensions;
using MESNET.Business.Application.Queries;
using MESNET.Business.Core.Enums;

namespace MESNET.Business.Application.Handlers;

public static class SearchNearbyBusinessesHandler
{
    public static async Task<IReadOnlyList<BusinessDto>> Handle(
        SearchNearbyBusinesses query, IQuerySession session)
    {
        var allActive = await session.Query<Core.Entities.Business>()
            .Where(b => b.Status == BusinessStatus.Active && b.Location != null)
            .ToListAsync();

        return allActive
            .Where(b => CalculateDistanceKm(
                query.Latitude, query.Longitude,
                b.Location!.Latitude, b.Location.Longitude) <= query.RadiusKm)
            .Select(b => b.ToDto())
            .ToList();
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
