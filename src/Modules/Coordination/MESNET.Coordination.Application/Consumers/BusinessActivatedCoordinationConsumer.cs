using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Coordination.Application.Helpers;
using MESNET.Coordination.Core.ReadModels;

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
            InstitutionId = @event.TenantId,
            ActiveStudentCount = 0,
        });
    }
}
