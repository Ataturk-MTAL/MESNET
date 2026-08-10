using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Coordination.Application.Helpers;
using MESNET.Coordination.Core.ReadModels;
using Microsoft.Extensions.Logging;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// İşletme yeniden aktif edildiğinde işletme düzeyi temel satırı yeniden oluşturur.
/// Alan satırları öğrenci yerleştirmeleriyle (StudentPlaced) yeniden kurulur.
/// </summary>
public static class BusinessActivatedCoordinationConsumer
{
    public static async Task Consume(
        BusinessActivated @event,
        IDocumentSession session,
        ILogger<BusinessCoordinationView> logger,
        CancellationToken cancellationToken)
    {
        var baseId = CoordinationViewId.Base(@event.BusinessId);
        var existing = await session.LoadAsync<BusinessCoordinationView>(baseId, cancellationToken);
        if (existing is not null) return;

        session.Store(new BusinessCoordinationView
        {
            Id = baseId,
            BusinessId = @event.BusinessId,
            Name = @event.Name,
            Address = @event.Address,
            District = AddressHelper.ExtractDistrict(@event.Address),
            Location = @event.Location,
            // Provenance → kapsam çevirimi ve bugünkü yaklaşımın sınırı BusinessScopeOrigin'de.
            InstitutionId = BusinessScopeOrigin.Resolve(
                @event.RegisteredByInstitutionId, @event.BusinessId, logger),
            ActiveStudentCount = 0,
        });
    }
}
