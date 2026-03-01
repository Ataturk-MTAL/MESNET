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
        // Kurum lokasyonunu al
        var institution = await session.LoadAsync<Institution.Core.Entities.Institution>(
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

        // Manuel mesafe girilmişleri filtrele, lokasyonu olanları ayır
        var autoViews = views.Where(v => !v.IsManualDistance && v.Location is not null).ToList();

        if (schoolLocation is null || autoViews.Count == 0) return;

        // OSRM batch API ile rota bazlı mesafe hesapla
        var destinations = autoViews.Select(v => v.Location!).ToList();
        var batchDistances = await osrmService.GetRouteDistancesBatchAsync(
            schoolLocation, destinations, cancellationToken);

        for (var i = 0; i < autoViews.Count; i++)
        {
            var view = autoViews[i];

            double distance;
            if (batchDistances.TryGetValue(i, out var osrmDistance))
            {
                // OSRM rota bazlı mesafe (gerçek yol)
                distance = osrmDistance;
            }
            else
            {
                // Fallback: Haversine (kuş uçuşu)
                distance = CoordinationCalculator.CalculateDistanceKm(schoolLocation, view.Location!);
            }

            view.DistanceToSchoolKm = distance;
            view.MaxCoordinationHours = CoordinationCalculator.CalculateMaxHours(distance, rules);
            session.Store(view);
        }
    }
}
