using Marten;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Enrollment.Shared.Events;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// Öğrenci kaydı silindiğinde BranchStudentCountView'u günceller.
/// Sınıf bazında öğrenci sayısını azaltır (minimum 0).
/// </summary>
public static class StudentDeregisteredCountConsumer
{
    public static async Task Consume(
        StudentDeregistered @event,
        IDocumentSession session,
        CancellationToken cancellationToken)
    {
        var id = BranchStudentCountView.CreateId(
            @event.InstitutionId, @event.AcademicPeriodId, @event.BranchCode, @event.EducationType);

        var view = await session.LoadAsync<BranchStudentCountView>(id, cancellationToken);
        if (view is null) return;

        view.StudentCountByClassYear.TryGetValue(@event.ClassYear, out var current);
        view.StudentCountByClassYear[@event.ClassYear] = Math.Max(0, current - 1);
        view.UpdatedAt = DateTime.UtcNow;

        session.Store(view);
    }
}
