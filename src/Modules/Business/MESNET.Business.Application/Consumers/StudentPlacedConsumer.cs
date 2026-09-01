using Marten;
using MESNET.Business.Core.ReadModels;
using MESNET.Business.Core.ValueObjects;
using MESNET.Business.Shared.Events;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Business.Application.Consumers;

public static class StudentPlacedConsumer
{
    /// <summary>
    /// Yerleştirme yaşam döngüsü olayı — canlı yol.
    /// </summary>
    public static Task<BusinessCapacityChanged?> Consume(StudentPlaced @event, IDocumentSession session)
        => Apply(@event.ToSnapshot(), session);

    /// <summary>
    /// Onarım yolu (#291): <c>POST /api/placements/resync-projections</c> bu olayı yayınlar.
    /// <c>StudentPlaced</c> yeniden yayınlanamaz — o, saga'nın başlatıcı olayıdır ve yeniden
    /// yayını tekil kısıt ihlaliyle ölü mektuba düşerdi (uç yine 200 dönerek).
    /// </summary>
    public static Task<BusinessCapacityChanged?> Consume(PlacementSnapshotResynced @event, IDocumentSession session)
        => Apply(@event, session);

    private static async Task<BusinessCapacityChanged?> Apply(
        PlacementSnapshotResynced @event, IDocumentSession session)
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

        // SAYI DEĞİL KÜME (#291). Eski hâli `CountAsync() + 1` idi ve bu, yalnız olay ilk kez
        // geldiğinde doğruydu: onarım yolunda bu yerleştirmenin satırı ZATEN sayılıyor, üstüne
        // bir de ekleniyordu — her koşuda kapasite bir artıyordu. Küme, bu olayın kendi
        // PlacementId'sini "henüz commit edilmemiş olabilir" diye eklerken mükerrer saymaz.
        // Canlı yolda sonuç değişmez: satır henüz yoksa küme yine N+1 verir.
        // Coordination.StudentPlacedConsumer.CountActiveStudentsAsync ile aynı desen.
        var activeIds = await session.Query<PlacedStudentView>()
            .Where(p => p.BusinessId == businessId && p.IsActive)
            .Select(p => p.Id)
            .ToListAsync();

        var placementIds = activeIds.ToHashSet();
        placementIds.Add(@event.PlacementId);
        var activeCount = placementIds.Count;

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
