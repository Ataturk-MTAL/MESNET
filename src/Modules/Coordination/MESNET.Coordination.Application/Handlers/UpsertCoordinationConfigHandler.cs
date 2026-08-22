using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Coordination.Application.Commands;
using MESNET.Coordination.Application.Errors;
using MESNET.Coordination.Core.Entities;
using MESNET.Coordination.Core.Services;

namespace MESNET.Coordination.Application.Handlers;

/// <summary>
/// Kurum koordinasyon yapılandırmasını oluşturur veya günceller.
///
/// <para>Yalnız <c>null</c> olmayan alanlar yazılır — kısmi güncelleme bilinçlidir.
/// Yazılacak alanlar önce <see cref="CoordinationConfigPolicy"/> ile doğrulanır (#134);
/// ihlalde hiçbir alan yazılmaz.</para>
/// </summary>
public static class UpsertCoordinationConfigHandler
{
    public static async Task Handle(
        UpsertCoordinationConfig command,
        IDocumentSession session,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        // Aktör token'dan gelir, istekten DEĞİL (#137).
        var updatedById = currentUser.GetUserId();

        var violation = CoordinationConfigPolicy.Validate(
            command.DistanceHourRules, command.MaxWeeklyExtraHours);

        if (violation is not null)
            throw new DomainException(CoordinationErrors.CoordinationConfigInvalid(violation));

        var existing = await session.Query<CoordinationConfig>()
            .FirstOrDefaultAsync(c => c.InstitutionId == command.InstitutionId, cancellationToken);

        if (existing is not null)
        {
            if (command.DistanceHourRules is not null)
                existing.DistanceHourRules = command.DistanceHourRules;
            if (command.IsMetropolitan.HasValue)
                existing.IsMetropolitan = command.IsMetropolitan.Value;
            if (command.MaxWeeklyExtraHours.HasValue)
                existing.MaxWeeklyExtraHours = command.MaxWeeklyExtraHours.Value;

            existing.UpdatedAt = DateTime.UtcNow;
            existing.UpdatedById = updatedById;
            session.Store(existing);
        }
        else
        {
            var config = new CoordinationConfig
            {
                Id = command.InstitutionId, // tek document per kurum
                InstitutionId = command.InstitutionId,
                UpdatedAt = DateTime.UtcNow,
                UpdatedById = updatedById,
            };

            if (command.DistanceHourRules is not null)
                config.DistanceHourRules = command.DistanceHourRules;
            if (command.IsMetropolitan.HasValue)
                config.IsMetropolitan = command.IsMetropolitan.Value;
            if (command.MaxWeeklyExtraHours.HasValue)
                config.MaxWeeklyExtraHours = command.MaxWeeklyExtraHours.Value;

            session.Store(config);
        }
    }
}
