using Marten;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// Öğrenci bir işletmeye yerleştirildiğinde BusinessCoordinationView'u günceller.
/// View normalde BusinessRegistered/BusinessApproved event'iyle zaten oluşturulmuş olmalıdır.
/// Fallback: view yoksa oluşturur (event sıralama garantisi yok).
/// </summary>
public static class StudentPlacedConsumer
{
    public static async Task Consume(
        StudentPlaced @event,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var existing = await session.LoadAsync<BusinessCoordinationView>(
            @event.BusinessId, cancellationToken);

        if (existing is null)
        {
            // Fallback: view henüz oluşturulmamış — event sıralama garantisi yok
            session.Store(new BusinessCoordinationView
            {
                Id = @event.BusinessId,
                Name = @event.BusinessName,
                BranchCode = @event.BranchCode,
                BranchName = @event.BranchName,
                InstitutionId = @event.InstitutionId,
                AcademicPeriodId = @event.AcademicPeriodId,
                ActiveStudentCount = 1,
            });
        }
        else
        {
            // View zaten var — öğrenci sayısını artır + alan bilgilerini güncelle
            existing.ActiveStudentCount++;
            existing.BranchCode = @event.BranchCode;
            existing.BranchName = @event.BranchName;
            existing.AcademicPeriodId = @event.AcademicPeriodId;
            session.Store(existing);
        }
    }
}
