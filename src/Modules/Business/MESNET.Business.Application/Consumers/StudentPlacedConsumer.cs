using Marten;
using MESNET.Business.Core.ReadModels;
using MESNET.Business.Core.ValueObjects;
using MESNET.Business.Shared.Events;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Business.Application.Consumers;

public static class StudentPlacedConsumer
{
    public static async Task<BusinessCapacityChanged?> Consume(
        StudentPlaced @event, IDocumentSession session)
    {
        // Okulda staj (#159): işletme yok — kapasite tüketilmez, işletmeye bağlı yerleştirme
        // kaydı da oluşmaz. Bu modülün bu olayla yapacağı hiçbir şey kalmıyor.
        if (@event.BusinessId is not { } businessId) return null;

        var view = new PlacedStudentView
        {
            Id = @event.PlacementId,
            StudentId = @event.StudentId,
            BusinessId = businessId,
            InstitutionId = @event.InstitutionId,
            PlacedAt = @event.PlacedAt,
            IsActive = true
        };

        session.Store(view);

        var business = await session.LoadAsync<Core.Entities.Business>(businessId);
        if (business is null) return null;

        var activeCount = await session.Query<PlacedStudentView>()
            .Where(p => p.BusinessId == businessId && p.IsActive)
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
