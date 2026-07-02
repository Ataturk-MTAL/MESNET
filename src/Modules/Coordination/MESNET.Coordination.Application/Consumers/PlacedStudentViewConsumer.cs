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
        session.Store(new CoordinationPlacedStudentView
        {
            Id = @event.PlacementId,
            StudentId = @event.StudentId,
            BusinessId = @event.BusinessId,
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
