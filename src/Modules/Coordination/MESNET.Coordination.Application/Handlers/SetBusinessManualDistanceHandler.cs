using Marten;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Application.Services;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Handlers;

public static class SetBusinessManualDistanceHandler
{
    public static async Task Handle(
        SetBusinessManualDistance command,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        if (command.DistanceKm <= 0)
            throw new DomainException(CoordinationErrors.InvalidDistance(command.DistanceKm));

        var view = await session.LoadAsync<BusinessCoordinationView>(
            command.BusinessId, cancellationToken);

        if (view is null)
            throw new DomainException(CoordinationErrors.BusinessNotFound(command.BusinessId));

        // Kurum config'den mesafe-saat kurallarını al
        var config = await session.LoadAsync<CoordinationConfig>(
            command.InstitutionId, cancellationToken);

        var rules = config?.DistanceHourRules ?? new CoordinationConfig().DistanceHourRules;

        view.DistanceToSchoolKm = command.DistanceKm;
        view.IsManualDistance = true;
        view.MaxCoordinationHours = CoordinationCalculator.CalculateMaxHours(command.DistanceKm, rules);

        session.Store(view);
    }
}
