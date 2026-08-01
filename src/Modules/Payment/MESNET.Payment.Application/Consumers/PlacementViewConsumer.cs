using Marten;
using MESNET.Contract.Shared.Events;
using MESNET.Enrollment.Shared.Events;
using MESNET.Payment.Core.ReadModels;
using MESNET.Payment.Core.Services;

namespace MESNET.Payment.Application.Consumers;

/// <summary>
/// Enrollment olaylarından Payment'ın yerel yerleştirme kaydını besler (#63).
/// </summary>
/// <remarks>
/// <b>Bu kayıt artık maaş dönemlerinin çalışma listesi DEĞİL (#154).</b> Liste
/// <c>ContractEmploymentView</c>'a taşındı: yerleştirme fesihte kapanıyor ve kapalı kayıt ay
/// sonu koşusundan düşüyordu, yani ay ortasında ayrılan öğrencinin çalıştığı günler için
/// ayrılınan işletmeye hiç maaş dönemi açılmıyordu. Sözleşme kaydı kapanmaz, yalnız bitiş
/// tarihi alır.
///
/// <para>Kayıt ve tüketici korunuyor: #152'nin regresyonlarını kilitleyen testler buna bağlı.
/// Payment içinde şu an okuyanı yok — kaldırılması ya da yeniden amaçlanması ayrı bir iştir.</para>
/// </remarks>
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
    public static Task Consume(ContractTerminated @event, IDocumentSession session)
        => CloseMatchingPlacementsAsync(
            session, @event.StudentId, @event.BusinessId, @event.TerminatedAt);

    /// <summary>
    /// Sözleşme başarıyla tamamlandığında yerleştirmeyi kapatır (#152).
    ///
    /// <para>Feshin <b>ikizi</b>: bu tüketici de eksikti ve aynı sonucu doğuruyordu. Stajını
    /// erken tamamlayan öğrenci için akademik dönem kapanana kadar her ay sonu maaş dönemi
    /// açılmaya devam ediyordu. <c>ContractWageConsumer</c> yine yalnız ücret görünümünü
    /// kapatıyor, yerleştirmeye dokunmuyordu.</para>
    /// </summary>
    public static Task Consume(ContractCompleted @event, IDocumentSession session)
        => CloseMatchingPlacementsAsync(
            session, @event.StudentId, @event.BusinessId, @event.CompletedAt);

    /// <summary>
    /// Sözleşme bitiş olaylarının ortak yazma yolu. Olaylar <c>PlacementId</c> taşımadığı için
    /// eşleşme öğrenci + işletme ikilisiyle yapılır; hangi kaydın kapanacağına saf
    /// <see cref="PlacementClosurePolicy"/> karar verir (tarih koruması dahil).
    /// </summary>
    private static async Task CloseMatchingPlacementsAsync(
        IDocumentSession session, Guid studentId, Guid businessId, DateTime endedAt)
    {
        var views = await session.Query<PlacementView>()
            .Where(p => p.StudentId == studentId
                     && p.BusinessId == businessId
                     && p.IsActive)
            .ToListAsync();

        foreach (var view in views)
        {
            if (!PlacementClosurePolicy.ShouldClose(
                    view.StudentId, view.BusinessId, view.IsActive, view.PlacedAt,
                    studentId, businessId, endedAt))
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
