using Marten;
using MESNET.Attendance.Core.ReadModels;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Attendance.Application.Consumers;

public static class StudentPlacedConsumer
{
    /// <summary>
    /// Yerleştirme yaşam döngüsü olayı — canlı yol.
    /// </summary>
    public static void Consume(StudentPlaced @event, IDocumentSession session)
        => Apply(@event.ToSnapshot(), session);

    /// <summary>
    /// Onarım yolu (#291): <c>POST /api/placements/resync-projections</c> bu olayı yayınlar.
    /// <c>StudentPlaced</c> yeniden yayınlanamaz — o, saga'nın başlatıcı olayıdır ve yeniden
    /// yayını tekil kısıt ihlaliyle ölü mektuba düşerdi (uç yine 200 dönerek).
    /// </summary>
    public static void Consume(PlacementSnapshotResynced @event, IDocumentSession session)
        => Apply(@event, session);

    private static void Apply(PlacementSnapshotResynced @event, IDocumentSession session)
    {
        session.Store(new InternshipPlacementView
        {
            Id = @event.PlacementId,
            StudentId = @event.StudentId,
            BusinessId = @event.BusinessId,
            InstitutionId = @event.InstitutionId,
            TeacherId = @event.TeacherId,
            AcademicPeriodId = @event.AcademicPeriodId
        });
    }
}
