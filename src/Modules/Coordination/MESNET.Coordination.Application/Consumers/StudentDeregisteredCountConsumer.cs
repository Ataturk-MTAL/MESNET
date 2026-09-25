using Marten;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Enrollment.Shared.Events;
using Wolverine.Configuration;
using Wolverine.Transports.Local;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// Öğrenci kaydı silindiğinde BranchStudentCountView'u günceller.
/// Sınıf bazında öğrenci sayısını azaltır (minimum 0).
/// </summary>
public sealed class StudentDeregisteredCountConsumer : IConfigureLocalQueue
{
    /// <summary>
    /// Sıralı kuyruk (#262): tüketici sayacı okuyup artırıp geri yazar. Paralel kuyrukta aynı
    /// alana ait olaylar aynı eski değeri okuyor ve artışlar birbirini eziyordu (ölçüldü: 40
    /// öğrenci varken sayaç 1/4/7).
    /// </summary>
    public static void Configure(LocalQueueConfiguration configuration)
    {
        configuration.Sequential();
    }

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
