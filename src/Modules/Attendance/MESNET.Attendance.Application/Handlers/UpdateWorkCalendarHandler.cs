using Marten;
using MESNET.Attendance.Application.Commands;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Core.Enums;
using MESNET.Attendance.Core.ValueObjects;
using MESNET.Attendance.Shared.Events;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;

namespace MESNET.Attendance.Application.Handlers;

public static class UpdateWorkCalendarHandler
{
    // Marten 9 senkron veri erişimini kaldırdı — .FirstOrDefault() burada
    // "As of Marten 9.0, only asynchronous data access is supported" fırlatıyordu (#73).
    public static async Task<(Guid, WorkCalendarUpdated)> Handle(
        UpdateWorkCalendar command, IDocumentSession session, ICurrentUserService currentUser)
    {
        // Aktör token'dan gelir, istekten DEĞİL (#137).
        var updatedById = currentUser.GetUserId();

        var calendar = await session.Query<WorkCalendar>()
            .FirstOrDefaultAsync(c => c.InstitutionId == command.InstitutionId && c.Year == command.Year);

        var restrictedDays = new List<CalendarDay>();
        foreach (var d in command.RestrictedDays)
        {
            if (!CalendarDayType.TryFromName(d.Type, true, out var type))
                throw new DomainException("ATTENDANCE_INVALID_DAY_TYPE",
                    $"Geçersiz takvim gün türü: {d.Type}.");
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
                UpdatedById = updatedById,
                UpdatedAt = DateTime.UtcNow
            };
        }
        else
        {
            calendar.RestrictedDays = restrictedDays;
            calendar.UpdatedById = updatedById;
            calendar.UpdatedAt = DateTime.UtcNow;
        }

        session.Store(calendar);

        var dayInfos = restrictedDays
            .Select(d => new CalendarDayInfo(d.Date, d.Type.Name, d.Description))
            .ToList();

        return (calendar.Id, new WorkCalendarUpdated(
            calendar.Id, calendar.InstitutionId, calendar.Year,
            restrictedDays.Count, updatedById, dayInfos));
    }
}
