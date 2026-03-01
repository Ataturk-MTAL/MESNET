using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MESNET.Common.Shared;
using Microsoft.Extensions.Logging;

namespace MESNET.Coordination.Application.Services;

/// <summary>
/// OSRM HTTP API ile rota bazlı mesafe hesaplama.
/// OSRM route API: GET /route/v1/driving/{lon1},{lat1};{lon2},{lat2}?overview=false
/// OSRM table API: GET /table/v1/driving/{lon1},{lat1};{lon2},{lat2};...?sources=0
/// </summary>
public sealed class OsrmDistanceService : IOsrmDistanceService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OsrmDistanceService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
    };

    public OsrmDistanceService(HttpClient httpClient, ILogger<OsrmDistanceService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<double?> GetRouteDistanceKmAsync(Location from, Location to, CancellationToken ct = default)
    {
        try
        {
            // OSRM format: longitude,latitude (NOT lat,lon)
            var coords = $"{from.Longitude},{from.Latitude};{to.Longitude},{to.Latitude}";
            var url = $"/route/v1/driving/{coords}?overview=false";

            var response = await _httpClient.GetFromJsonAsync<OsrmRouteResponse>(url, JsonOptions, ct);

            if (response?.Code != "Ok" || response.Routes is not { Count: > 0 })
            {
                _logger.LogWarning("OSRM rota bulunamadi: {From} -> {To}", from, to);
                return null;
            }

            // OSRM mesafe metre cinsindendir, km'ye çevir
            var distanceKm = response.Routes[0].Distance / 1000.0;
            return Math.Round(distanceKm, 2);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OSRM mesafe hesaplanamadi: {From} -> {To}", from, to);
            return null;
        }
    }

    public async Task<Dictionary<int, double>> GetRouteDistancesBatchAsync(
        Location source,
        IReadOnlyList<Location> destinations,
        CancellationToken ct = default)
    {
        var result = new Dictionary<int, double>();

        if (destinations.Count == 0) return result;

        try
        {
            // OSRM table API: sources=0 → ilk koordinat kaynak, geri kalanı hedef
            var coordParts = new List<string>
            {
                $"{source.Longitude},{source.Latitude}",
            };
            foreach (var dest in destinations)
            {
                coordParts.Add($"{dest.Longitude},{dest.Latitude}");
            }

            var coords = string.Join(";", coordParts);
            var url = $"/table/v1/driving/{coords}?sources=0&annotations=distance";

            var response = await _httpClient.GetFromJsonAsync<OsrmTableResponse>(url, JsonOptions, ct);

            if (response?.Code != "Ok" || response.Distances is not { Count: > 0 })
            {
                _logger.LogWarning("OSRM table sorgusu basarisiz, {Count} hedef", destinations.Count);
                return result;
            }

            // distances[0] → source'tan tüm noktalara mesafeler (metre)
            var row = response.Distances[0];
            // index 0 = source→source (0), index 1..N = source→destination[0..N-1]
            for (var i = 1; i < row.Count && i - 1 < destinations.Count; i++)
            {
                var distMeters = row[i];
                if (distMeters is not null and not double.NaN)
                {
                    result[i - 1] = Math.Round(distMeters.Value / 1000.0, 2);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OSRM batch mesafe hesaplanamadi, {Count} hedef", destinations.Count);
        }

        return result;
    }

    // ── OSRM Response DTOs ──

    private sealed class OsrmRouteResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("routes")]
        public List<OsrmRoute> Routes { get; set; } = [];
    }

    private sealed class OsrmRoute
    {
        [JsonPropertyName("distance")]
        public double Distance { get; set; } // metre

        [JsonPropertyName("duration")]
        public double Duration { get; set; } // saniye
    }

    private sealed class OsrmTableResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("distances")]
        public List<List<double?>> Distances { get; set; } = [];
    }
}
