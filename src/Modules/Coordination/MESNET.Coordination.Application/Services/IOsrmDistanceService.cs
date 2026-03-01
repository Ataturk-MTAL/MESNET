using MESNET.Common.Shared;

namespace MESNET.Coordination.Application.Services;

/// <summary>
/// OSRM (Open Source Routing Machine) ile rota bazlı mesafe hesaplama servisi.
/// Kuş uçuşu değil, gerçek yol mesafesini hesaplar.
/// </summary>
public interface IOsrmDistanceService
{
    /// <summary>
    /// İki nokta arasındaki rota bazlı mesafeyi km cinsinden hesaplar.
    /// OSRM erişilemezse null döner.
    /// </summary>
    Task<double?> GetRouteDistanceKmAsync(Location from, Location to, CancellationToken ct = default);

    /// <summary>
    /// Bir kaynak noktadan birden çok hedefe rota bazlı mesafe hesaplar (batch).
    /// OSRM table API kullanır — tek istek ile N mesafe.
    /// Key: hedef index, Value: mesafe km.
    /// </summary>
    Task<Dictionary<int, double>> GetRouteDistancesBatchAsync(
        Location source,
        IReadOnlyList<Location> destinations,
        CancellationToken ct = default);
}
