using JasperFx;
using Marten;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Enrollment.Shared.Events;
using Wolverine.ErrorHandling;
using Wolverine.Runtime.Handlers;
using Wolverine.Configuration;
using Wolverine.Transports.Local;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// Yeni öğrenci kaydedildiğinde BranchStudentCountView'u günceller.
/// Sınıf bazında öğrenci sayısını artırır.
///
/// Kuyruk sıralıdır (bkz. <see cref="Configure(LocalQueueConfiguration)"/>). RetryOnce, sıralı
/// kuyruktan önceki paralel dönemden kalma bir güvencedir: aynı anda ilk INSERT'e giden iki
/// olayın ikincisi DocumentAlreadyExistsException alır, yeniden denemede kaydı bulup günceller.
/// </summary>
public sealed class StudentRegisteredCountConsumer : IConfigureLocalQueue
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

    public static void Configure(HandlerChain chain)
    {
        chain.OnException<DocumentAlreadyExistsException>()
            .RetryOnce();
    }

    public static async Task Consume(
        StudentRegistered @event,
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
                StudentCountByClassYear = new Dictionary<int, int> { [@event.ClassYear] = 1 },
                UpdatedAt = DateTime.UtcNow
            };
        }
        else
        {
            view.StudentCountByClassYear.TryGetValue(@event.ClassYear, out var current);
            view.StudentCountByClassYear[@event.ClassYear] = current + 1;
            view.UpdatedAt = DateTime.UtcNow;
        }

        session.Store(view);
    }
}
