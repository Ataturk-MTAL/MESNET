using Marten;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Core.ValueObjects;
using MESNET.Attendance.Shared.Events;

namespace MESNET.Attendance.Application.Handlers;

public static class UpdateWorkCalendarHandler
{
    public static WorkCalendarUpdated Handle(UpdateWorkCalendar command, IDocumentSession session)
    {
        var calendar = session.Query<WorkCalendar>()
            .FirstOrDefault(c => c.InstitutionId == command.InstitutionId && c.Year == command.Year);

        var restrictedDays = command.RestrictedDays.Select(d =>
        {
            if (!CalendarDayType.TryFromName(d.Type, true, out var type))
                throw new InvalidOperationException($"Geçersiz takvim gün türü: {d.Type}");
            return new CalendarDay(d.Date, type, d.Description);
        }).ToList();

        if (calendar is null)
        {
            calendar = new WorkCalendar
            {
                Id = Guid.NewGuid(),
                InstitutionId = command.InstitutionId,
                Year = command.Year,
                RestrictedDays = restrictedDays,
                UpdatedBy = command.UpdatedBy,
                UpdatedAt = DateTime.UtcNow
            };
        }
        else
        {
            calendar.RestrictedDays = restrictedDays;
            calendar.UpdatedBy = command.UpdatedBy;
            calendar.UpdatedAt = DateTime.UtcNow;
        }

        session.Store(calendar);

        return new WorkCalendarUpdated(
            calendar.Id, calendar.InstitutionId, calendar.Year,
            restrictedDays.Count, command.UpdatedBy);
    }
}
