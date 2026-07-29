using Marten;
using MESNET.Contract.Shared.Events;
using MESNET.Enrollment.Shared.Events;
using MESNET.Payment.Core.ReadModels;
using MESNET.Payment.Core.Services;

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
            PlacedAt = @event.PlacedAt,
            IsActive = true
        });
    }

    /// <summary>
    /// Sözleşme feshedildiğinde yerleştirmeyi kapatır (#152).
    ///
    /// <para><b>Bu tüketici EKSİKTİ.</b> Fesih olayını yalnız <c>InternshipSaga</c> ve
    /// <c>ContractWageConsumer</c> dinliyordu; ikincisi <c>StudentContractWageView</c>'ı
    /// kapatıyor ama <c>PlacementView</c>'a dokunmuyordu. Sonuç: ay sonu maaş zamanlayıcısı
    /// (<c>OpenMonthlySalaryPeriodsHandler</c>) ayrılmış öğrenciyi hâlâ aktif görüyor,
    /// ayrıldığı işletmeye dekont yükümlülüğü doğuyor ve ayın 8'inde gecikme uyarısı
    /// gidiyordu.</para>
    ///
    /// <para>Daha kötüsü: <c>SalaryPeriodId</c> (öğrenci, ay) ikilisinden türetildiği için,
    /// aynı ay içinde fesih + yeni yerleştirme olan öğrencide bayat eski kayıt önce işlenip
    /// maaş dönemini ESKİ işletmeyle açıyor, yeni yerleştirme "zaten var" diye atlanıyordu.
    /// Ayın maaşı öğrencinin ayrıldığı işletmeye yazılıyordu.</para>
    /// </summary>
    public static async Task Consume(ContractTerminated @event, IDocumentSession session)
    {
        // Fesih olayı PlacementId taşımaz; eşleşme öğrenci + işletme ikilisiyle yapılır.
        var views = await session.Query<PlacementView>()
            .Where(p => p.StudentId == @event.StudentId
                     && p.BusinessId == @event.BusinessId
                     && p.IsActive)
            .ToListAsync();

        foreach (var view in views)
        {
            // Karar saf politikada — bkz. PlacementClosurePolicy (tarih koruması dahil).
            if (!PlacementClosurePolicy.ShouldClose(
                    view.StudentId, view.BusinessId, view.IsActive, view.PlacedAt,
                    @event.StudentId, @event.BusinessId, @event.TerminatedAt))
            {
                continue;
            }

            view.IsActive = false;
            session.Store(view);
        }
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
