using Marten;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// Öğrenci bir işletmeye yerleştirildiğinde öğrenci-bazlı CoordinationPlacedStudentView oluşturur.
/// İşletmenin not girişinde "kendi öğrencileri"ni listelemesi ve yerleştirme doğrulaması için.
/// (BusinessCoordinationView'i güncelleyen StudentPlacedConsumer'dan ayrı — tek sorumluluk.)
/// </summary>
public static class PlacedStudentViewConsumer
{
    public static void Consume(StudentPlaced @event, IDocumentSession session)
    {
        // Okulda staj (#159): bu görünüm işletme kapsamlıdır — koordinasyon ekranları
        // ziyaret edilecek işletmeyi listeler. İşletmesiz yerleştirme buraya girmez.
        // NOT: dönem notu akışının okulda staj hâlinde kimden geleceği ayrı bir karardır
        // (bugün notu işletme giriyor); issue'da açık madde olarak duruyor.
        if (@event.BusinessId is not { } businessId) return;

        session.Store(new CoordinationPlacedStudentView
        {
            Id = @event.PlacementId,
            StudentId = @event.StudentId,
            BusinessId = businessId,
            InstitutionId = @event.InstitutionId,
            AcademicPeriodId = @event.AcademicPeriodId,
            TeacherId = @event.TeacherId,
            StudentName = @event.StudentName,
            BranchCode = @event.BranchCode,
            BranchName = @event.BranchName,
            PlacedAt = @event.PlacedAt,
            IsActive = true,
        });
    }
}
