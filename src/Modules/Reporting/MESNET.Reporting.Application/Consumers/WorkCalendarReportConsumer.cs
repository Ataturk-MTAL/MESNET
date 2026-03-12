using Marten;
using MESNET.Attendance.Shared.Events;
using MESNET.Reporting.Core.ReadModels;

namespace MESNET.Reporting.Application.Consumers;

/// <summary>
/// Attendance modülü WorkCalendarUpdated event'ini dinleyerek
/// WorkCalendarReportView read model'ini günceller.
/// Aylık devamsızlık formunda tatil/iş günü hesaplaması için kullanılır.
/// </summary>
public static class WorkCalendarReportConsumer
{
    public static async Task Consume(WorkCalendarUpdated @event, IDocumentSession session)
    {
        var view = await session.LoadAsync<WorkCalendarReportView>(@event.CalendarId)
                   ?? new WorkCalendarReportView
                   {
                       Id = @event.CalendarId,
                       InstitutionId = @event.InstitutionId,
                       Year = @event.Year
                   };

        view.RestrictedDays = @event.RestrictedDays?
            .Select(d => new CalendarDayEntry(d.Date, d.Type, d.Description))
            .ToList() ?? [];

        session.Store(view);
    }
}
