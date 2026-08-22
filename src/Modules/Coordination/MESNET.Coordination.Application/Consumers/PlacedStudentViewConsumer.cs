using Marten;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// Yerleştirilen öğrenciyi not girişi ekranlarının okuduğu görünüme yazar.
/// (BusinessCoordinationView'i güncelleyen StudentPlacedConsumer'dan ayrı — tek sorumluluk.)
///
/// <para>Yerleştirme <b>iki ayrı görünüme</b> gider: işletmeli olan
/// <see cref="CoordinationPlacedStudentView"/>'a, işverensiz olan (#159)
/// <see cref="SchoolPlacedStudentView"/>'a. Tek görünümde birleştirilseydi işletme kapsamlı
/// her sorgu okulda staj satırlarını da toplardı.</para>
/// </summary>
public static class PlacedStudentViewConsumer
{
    public static void Consume(StudentPlaced @event, IDocumentSession session)
    {
        // Okulda staj (#159): işletme kapsamlı görünüme girmez, kendi görünümüne yazılır.
        // Bu satır olmadan öğrenci not giriş listesinde hiç görünmüyordu ve dönem notu
        // hiç girilemiyordu (#171).
        if (@event.BusinessId is not { } businessId)
        {
            session.Store(new SchoolPlacedStudentView
            {
                Id = @event.PlacementId,
                StudentId = @event.StudentId,
                InstitutionId = @event.InstitutionId,
                AcademicPeriodId = @event.AcademicPeriodId,
                TeacherId = @event.TeacherId,
                StudentName = @event.StudentName,
                BranchCode = @event.BranchCode,
                BranchName = @event.BranchName,
                PlacedAt = @event.PlacedAt,
                IsActive = true,
            });
            return;
        }

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
