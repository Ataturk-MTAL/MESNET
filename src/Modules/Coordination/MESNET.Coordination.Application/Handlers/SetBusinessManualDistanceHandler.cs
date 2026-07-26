using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Application.Helpers;
using MESNET.Coordination.Application.Services;
using MESNET.Coordination.Core.Entities;

namespace MESNET.Coordination.Application.Handlers;

/// <summary>
/// Manuel mesafe girişi <b>işletme düzeyi</b> bir işlemdir: mesafe ve ondan türeyen
/// azami koordinatörlük saati, işletmenin temel satırına ve <b>tüm alan satırlarına</b>
/// yazılır. Tavan her alan için ayrı uygulanır — 7 km'deki işletmeye EET de BT de
/// aynı üst sınıra kadar takdir edebilir (#114).
/// </summary>
public static class SetBusinessManualDistanceHandler
{
    public static async Task Handle(
        SetBusinessManualDistance command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        if (command.DistanceKm <= 0)
            throw new DomainException(CoordinationErrors.InvalidDistance(command.DistanceKm));

        var rows = await CoordinationViewLookup.LoadAllRowsAsync(
            session, command.BusinessId, cancellationToken);

        if (rows.Count == 0)
            throw new DomainException(CoordinationErrors.BusinessNotFound(command.BusinessId));

        // Kurum config'den mesafe-saat kurallarını al
        var config = await session.LoadAsync<CoordinationConfig>(
            command.InstitutionId, cancellationToken);

        var rules = config?.DistanceHourRules ?? new CoordinationConfig().DistanceHourRules;
        var maxHours = CoordinationCalculator.CalculateMaxHours(command.DistanceKm, rules);

        foreach (var row in rows)
        {
            row.DistanceToSchoolKm = command.DistanceKm;
            row.IsManualDistance = true;
            row.MaxCoordinationHours = maxHours;
            session.Store(row);
        }
    }
}
