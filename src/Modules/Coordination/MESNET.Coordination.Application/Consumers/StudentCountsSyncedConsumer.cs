using Marten;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// Enrollment modülünden gelen toplu senkronizasyon event'ini consume eder.
/// BranchStudentCountView'u tamamen değiştirir (increment değil, replace).
/// </summary>
public static class StudentCountsSyncedConsumer
{
    public static async Task Consume(
        StudentCountsSynced @event,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var id = BranchStudentCountView.CreateId(
            @event.InstitutionId, @event.AcademicPeriodId, @event.BranchCode, @event.EducationType);

        var view = await session.LoadAsync<BranchStudentCountView>(id, cancellationToken);

        if (view is null)
        {
            view = new BranchStudentCountView
            {
                Id = id,
                InstitutionId = @event.InstitutionId,
                AcademicPeriodId = @event.AcademicPeriodId,
                BranchCode = @event.BranchCode,
                EducationType = @event.EducationType,
                StudentCountByClassYear = new Dictionary<int, int>(@event.CountsByClassYear),
                UpdatedAt = DateTime.UtcNow
            };
        }
        else
        {
            view.StudentCountByClassYear = new Dictionary<int, int>(@event.CountsByClassYear);
            view.UpdatedAt = DateTime.UtcNow;
        }

        session.Store(view);
    }
}
