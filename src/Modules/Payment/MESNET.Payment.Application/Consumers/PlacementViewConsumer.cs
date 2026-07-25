using Marten;
using MESNET.Enrollment.Shared.Events;
using MESNET.Payment.Core.ReadModels;

namespace MESNET.Payment.Application.Consumers;

/// <summary>
/// Enrollment olaylarından Payment'ın yerel yerleştirme kaydını besler.
/// Aylık maaş zamanlayıcısının çalışma listesi budur (#63).
/// </summary>
public static class PlacementViewConsumer
{
    public static void Consume(StudentPlaced @event, IDocumentSession session)
    {
        session.Store(new PlacementView
        {
            Id = @event.PlacementId,
            StudentId = @event.StudentId,
            BusinessId = @event.BusinessId,
            InstitutionId = @event.InstitutionId,
            AcademicPeriodId = @event.AcademicPeriodId,
            IsActive = true
        });
    }

    public static async Task Consume(StudentFailedToComplete @event, IDocumentSession session)
    {
        var view = await session.LoadAsync<PlacementView>(@event.PlacementId);
        if (view is null) return;

        view.IsActive = false;
        session.Store(view);
    }

    /// <remarks>
    /// <c>StudentDeregistered</c> yerleştirme kimliği taşımıyor, yalnız <c>StudentId</c> var —
    /// bu yüzden öğrencinin tüm aktif yerleştirmeleri kapatılıyor.
    /// </remarks>
    public static async Task Consume(StudentDeregistered @event, IDocumentSession session)
    {
        var views = await session.Query<PlacementView>()
            .Where(p => p.StudentId == @event.StudentId && p.IsActive)
            .ToListAsync();

        foreach (var view in views)
        {
            view.IsActive = false;
            session.Store(view);
        }
    }
}
