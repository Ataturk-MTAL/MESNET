using Marten;
using MESNET.Coordination.Core.ReadModels;
using MESNET.Institution.Shared.Events;

namespace MESNET.Coordination.Application.Consumers;

/// <summary>
/// Müdür/müdür yardımcısı dönem sonu not giriş penceresini açtığında (Institution.GradeEntryWindowSet),
/// Coordination'daki AcademicPeriodView'in pencere tarihlerini günceller. Not giriş handler'ı bu
/// pencereyi kontrol eder.
/// </summary>
public static class GradeEntryWindowSetConsumer
{
    public static async Task Consume(GradeEntryWindowSet @event, IDocumentSession session)
    {
        var view = await session.LoadAsync<AcademicPeriodView>(@event.AcademicPeriodId)
                   ?? new AcademicPeriodView
                   {
                       Id = @event.AcademicPeriodId,
                       InstitutionId = @event.InstitutionId,
                       Name = string.Empty,
                       IsActive = true
                   };

        view.GradeEntryStartDate = @event.StartDate;
        view.GradeEntryEndDate = @event.EndDate;
        session.Store(view);
    }
}
