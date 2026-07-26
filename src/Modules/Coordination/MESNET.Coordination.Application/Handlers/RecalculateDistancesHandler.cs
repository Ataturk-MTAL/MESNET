using Marten;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Services;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class RecalculateDistancesHandler
{
    public static async Task Handle(
        RecalculateDistances command,
        IDocumentSession session,
        IOsrmDistanceService osrmService,
        CancellationToken cancellationToken)
    {
        // Kurum lokasyonunu al — event-tabanlı InstitutionView
        var institution = await session.LoadAsync<InstitutionView>(
            command.InstitutionId, cancellationToken);

        var schoolLocation = institution?.Location;

        // Kurum config'den mesafe-saat kurallarını al
        var config = await session.LoadAsync<CoordinationConfig>(
            command.InstitutionId, cancellationToken);

        var rules = config?.DistanceHourRules ?? new CoordinationConfig().DistanceHourRules;

        // Tüm işletme view'larını al
        var views = await session.Query<BusinessCoordinationView>()
            .Where(v => v.InstitutionId == command.InstitutionId)
            .ToListAsync(cancellationToken);

        // Manuel mesafe girilmişleri filtrele, lokasyonu olanları ayır.
        // Mesafe işletme geneli bir değerdir: aynı işletmenin tüm alan satırları tek bir
        // OSRM sorgusuyla hesaplanıp aynı değeri alır (#114).
        var autoGroups = views
            .Where(v => !v.IsManualDistance && v.Location is not null)
            .GroupBy(v => v.ResolveBusinessId())
            .ToList();

        if (schoolLocation is null || autoGroups.Count == 0) return;

        // OSRM batch API ile rota bazlı mesafe hesapla
        var destinations = autoGroups.Select(g => g.First().Location!).ToList();
        var batchDistances = await osrmService.GetRouteDistancesBatchAsync(
            schoolLocation, destinations, cancellationToken);

        for (var i = 0; i < autoGroups.Count; i++)
        {
            var group = autoGroups[i];

            double distance;
            if (batchDistances.TryGetValue(i, out var osrmDistance))
            {
                // OSRM rota bazlı mesafe (gerçek yol)
                distance = osrmDistance;
            }
            else
            {
                // Fallback: Haversine (kuş uçuşu)
                distance = CoordinationCalculator.CalculateDistanceKm(
                    schoolLocation, group.First().Location!);
            }

            var maxHours = CoordinationCalculator.CalculateMaxHours(distance, rules);

            foreach (var view in group)
            {
                view.DistanceToSchoolKm = distance;
                view.MaxCoordinationHours = maxHours;
                session.Store(view);
            }
        }
    }
}
