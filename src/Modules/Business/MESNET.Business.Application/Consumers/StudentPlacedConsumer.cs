using Marten;
using MESNET.Business.Application.ReadModels;
using MESNET.Business.Core.ValueObjects;
using MESNET.Business.Shared.Events;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Business.Application.Consumers;

public static class StudentPlacedConsumer
{
    public static async Task<BusinessCapacityChanged?> Consume(
        StudentPlaced @event, IDocumentSession session)
    {
        var view = new PlacedStudentView
        {
            Id = @event.PlacementId,
            StudentId = @event.StudentId,
            BusinessId = @event.BusinessId,
            InstitutionId = @event.InstitutionId,
            PlacedAt = @event.PlacedAt,
            IsActive = true
        };

        session.Store(view);

        var business = await session.LoadAsync<Core.Entities.Business>(@event.BusinessId);
        if (business is null) return null;

        var activeCount = await session.Query<PlacedStudentView>()
            .Where(p => p.BusinessId == @event.BusinessId && p.IsActive)
            .CountAsync();

        // +1 for the current placement that hasn't been committed yet
        activeCount++;

        business.Capacity = new BusinessCapacity
        {
            TotalSlots = business.Capacity.TotalSlots,
            OccupiedSlots = activeCount
        };

        session.Store(business);

        return new BusinessCapacityChanged(
            business.Id,
            business.Capacity.TotalSlots,
            business.Capacity.OccupiedSlots);
    }
}
