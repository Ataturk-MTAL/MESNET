namespace MESNET.Business.Application.Queries;

public sealed record SearchNearbyBusinesses(
    double Latitude,
    double Longitude,
    double RadiusKm);
