using Marten;
using MESNET.Coordination.Application.Services;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Helpers;

/// <summary>
/// Tekil işletme için mesafe hesaplama — consumer'larda ortak kullanılır.
/// OSRM → Haversine fallback → config rules → MaxCoordinationHours.
/// </summary>
public static class DistanceHelper
{
    public static async Task CalculateAndSetDistanceAsync(
        BusinessCoordinationView view,
        Guid institutionId,
        IDocumentSession session,
        IOsrmDistanceService osrmService,
        CancellationToken cancellationToken)
    {
        if (view.Location is null) return;

        // Kurum lokasyonunu al — event-tabanlı InstitutionView
        var institution = await session.LoadAsync<InstitutionView>(
            institutionId, cancellationToken);

        var schoolLocation = institution?.Location;
        if (schoolLocation is null) return;

        // OSRM ile rota bazlı mesafe, fallback: Haversine
        var osrmDistance = await osrmService.GetRouteDistanceKmAsync(
            schoolLocation, view.Location, cancellationToken);

        var distance = osrmDistance
                       ?? CoordinationCalculator.CalculateDistanceKm(schoolLocation, view.Location);

        // Config'den mesafe-saat kurallarını al
        var config = await session.LoadAsync<CoordinationConfig>(
            institutionId, cancellationToken);

        var rules = config?.DistanceHourRules ?? new CoordinationConfig().DistanceHourRules;

        view.DistanceToSchoolKm = distance;
        view.MaxCoordinationHours = CoordinationCalculator.CalculateMaxHours(distance, rules);
    }

    /// <summary>
    /// Mesafeden türeyen alanları bir satırdan diğerine kopyalar.
    /// Mesafe işletme geneli bir değerdir; azami saat tavanı <b>her alan için ayrı</b>
    /// uygulanır, bu yüzden aynı tavan her alan satırına kopyalanır.
    /// </summary>
    public static void CopyDistanceTo(BusinessCoordinationView source, BusinessCoordinationView target)
    {
        if (ReferenceEquals(source, target)) return;

        target.DistanceToSchoolKm = source.DistanceToSchoolKm;
        target.IsManualDistance = source.IsManualDistance;
        target.MaxCoordinationHours = source.MaxCoordinationHours;
    }
}
