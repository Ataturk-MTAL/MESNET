using Marten;
using MESNET.Attendance.Core.ReadModels;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Attendance.Application.Consumers;

public static class StudentPlacedConsumer
{
    public static void Consume(StudentPlaced @event, IDocumentSession session)
    {
        session.Store(new InternshipPlacementView
        {
            Id = @event.PlacementId,
            StudentId = @event.StudentId,
            BusinessId = @event.BusinessId,
            InstitutionId = @event.InstitutionId,
            TeacherId = @event.TeacherId
        });
    }
}
