using MESNET.Common.Shared;
using MESNET.Coordination.Core.Entities;

namespace MESNET.Coordination.Application.Services;

/// <summary>
/// Koordinatörlük mesafe ve saat hesaplama fonksiyonları.
/// </summary>
public static class CoordinationCalculator
{
    /// <summary>
    /// Mesafeye göre verilebilecek maksimum koordinatörlük saatini hesaplar.
    /// Mevzuat: ≤1km→2s, ≤3km→4s, ≤5km→6s, >5km→8s
    /// </summary>
    public static int CalculateMaxHours(double distanceKm, List<DistanceHourRule> rules)
    {
        foreach (var rule in rules.OrderBy(r => r.MaxDistanceKm))
        {
            if (distanceKm <= rule.MaxDistanceKm) return rule.Hours;
        }

        return rules.Last().Hours;
    }

    /// <summary>
    /// İki konum arasındaki mesafeyi Haversine formülüyle hesaplar (km).
    /// Fallback olarak kullanılır — OSRM erişilemezse.
    /// </summary>
    public static double CalculateDistanceKm(Location from, Location to)
    {
        const double earthRadiusKm = 6371.0;

        var dLat = ToRadians(to.Latitude - from.Latitude);
        var dLon = ToRadians(to.Longitude - from.Longitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(from.Latitude)) *
                Math.Cos(ToRadians(to.Latitude)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return Math.Round(earthRadiusKm * c, 2);
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
