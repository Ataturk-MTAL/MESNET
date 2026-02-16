using Marten;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Shared.Events;

namespace MESNET.Attendance.Application.Handlers;

public static class CheckAttendanceLimitHandler
{
    public static AttendanceLimitExceeded? Handle(
        AttendanceMarked @event, IQuerySession session)
    {
        var view = session.Query<AttendanceView>()
            .FirstOrDefault(v => v.StudentId == @event.StudentId
                && v.BusinessId == @event.BusinessId);

        const int limit = 20;
        var total = (view?.UnexcusedDays ?? 0) + 1;

        if (total >= limit)
            return new AttendanceLimitExceeded(
                @event.StudentId, @event.InstitutionId, @event.BusinessId, total, limit);

        return null;
    }
}
