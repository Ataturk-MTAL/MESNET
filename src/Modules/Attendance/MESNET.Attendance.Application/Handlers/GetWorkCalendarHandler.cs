using Marten;
using MESNET.Attendance.Application.Dtos;
using MESNET.Attendance.Application.Errors;
using MESNET.Attendance.Application.Extensions;
using MESNET.Attendance.Application.Queries;
using MESNET.Attendance.Core.Entities;
using MESNET.Common.Shared;

namespace MESNET.Attendance.Application.Handlers;

public static class GetWorkCalendarHandler
{
    public static async Task<WorkCalendarDto> Handle(GetWorkCalendar query, IQuerySession session)
    {
        var calendar = await session.Query<WorkCalendar>()
            .FirstOrDefaultAsync(c => c.InstitutionId == query.InstitutionId && c.Year == query.Year);

        if (calendar is null)
            throw new DomainException(AttendanceErrors.CalendarNotFound(query.InstitutionId, query.Year));

        return calendar.ToDto();
    }
}
