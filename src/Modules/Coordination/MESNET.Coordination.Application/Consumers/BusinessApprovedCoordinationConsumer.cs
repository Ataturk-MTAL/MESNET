using Marten;
using MESNET.Business.Shared.Events;
using MESNET.Coordination.Application.Helpers;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// Self-register işletme onaylandığında işletme düzeyi temel satırı oluşturur.
/// </summary>
public static class BusinessApprovedCoordinationConsumer
{
    public static async Task Consume(
        BusinessApproved @event,
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
            InstitutionId = @event.InstitutionId,
            ActiveStudentCount = 0,
        });
    }
}
