using Marten;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Core.ValueObjects;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Shared;

namespace MESNET.Attendance.Application.Handlers;

public static class UpdateWorkCalendarHandler
{
    public static Result<WorkCalendarUpdated> Handle(UpdateWorkCalendar command, IDocumentSession session)
    {
        var calendar = session.Query<WorkCalendar>()
            .FirstOrDefault(c => c.InstitutionId == command.InstitutionId && c.Year == command.Year);

        var restrictedDays = new List<CalendarDay>();
        foreach (var d in command.RestrictedDays)
        {
            if (!CalendarDayType.TryFromName(d.Type, true, out var type))
                return Result<WorkCalendarUpdated>.Failure(
                    AttendanceErrors.OperationFailed("UpdateCalendar", $"Geçersiz takvim gün türü: {d.Type}"));
            restrictedDays.Add(new CalendarDay(d.Date, type, d.Description));
        }

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

        return Result<WorkCalendarUpdated>.Success(new WorkCalendarUpdated(
            calendar.Id, calendar.InstitutionId, calendar.Year,
            restrictedDays.Count, command.UpdatedBy));
    }
}
