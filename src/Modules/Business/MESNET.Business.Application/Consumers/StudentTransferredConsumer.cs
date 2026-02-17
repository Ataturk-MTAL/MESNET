using Marten;
using MESNET.Business.Core.ReadModels;
using MESNET.Business.Core.ValueObjects;
using MESNET.Business.Shared.Events;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Business.Application.Consumers;

public static class StudentTransferredConsumer
{
    public static async Task<BusinessCapacityChanged?> Consume(
        StudentTransferred @event, IDocumentSession session)
    {
        var placement = await session.LoadAsync<PlacedStudentView>(@event.PlacementId);
        if (placement is not null)
        {
            placement.IsActive = false;
            placement.TransferredAt = DateTime.UtcNow;
            session.Store(placement);
        }

        var oldBusiness = await session.LoadAsync<Core.Entities.Business>(@event.OldBusinessId);
        if (oldBusiness is null) return null;

        var activeCount = await session.Query<PlacedStudentView>()
            .Where(p => p.BusinessId == @event.OldBusinessId && p.IsActive)
            .CountAsync();

        // -1 for the current placement that hasn't been committed yet
        activeCount = Math.Max(0, activeCount - 1);

        oldBusiness.Capacity = new BusinessCapacity
        {
            TotalSlots = oldBusiness.Capacity.TotalSlots,
            OccupiedSlots = activeCount
        };

        session.Store(oldBusiness);

        return new BusinessCapacityChanged(
            oldBusiness.Id,
            oldBusiness.Capacity.TotalSlots,
            oldBusiness.Capacity.OccupiedSlots);
    }
}
