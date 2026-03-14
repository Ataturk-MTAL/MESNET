using Marten;
using Marten.Exceptions;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Enrollment.Shared.Events;
using Wolverine.ErrorHandling;
using Wolverine.Runtime.Handlers;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// Yeni öğrenci kaydedildiğinde BranchStudentCountView'u günceller.
/// Sınıf bazında öğrenci sayısını artırır.
///
/// Not: Concurrent StudentRegistered event'leri aynı branch için paralel çalışabilir.
/// Marten Store() ilk kez INSERT yaparken ikinci handler aynı ID ile INSERT deneyince
/// DocumentAlreadyExistsException alır. Wolverine retry mekanizması ile yeni session'da
/// LoadAsync mevcut kaydı bulur ve UPDATE yapar.
/// </summary>
public static class StudentRegisteredCountConsumer
{
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
