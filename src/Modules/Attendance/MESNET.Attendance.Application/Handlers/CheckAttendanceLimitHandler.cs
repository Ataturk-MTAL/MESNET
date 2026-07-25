using Marten;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Shared.Events;

namespace MESNET.Attendance.Application.Handlers;

public static class CheckAttendanceLimitHandler
{
    // Marten 9 senkron veri erişimini kaldırdı — .FirstOrDefault() burada
    // "As of Marten 9.0, only asynchronous data access is supported" fırlatıyordu ve
    // AttendanceMarked dead letter'a düşüyordu, yani devamsızlık limiti hiç kontrol edilmiyordu (#73).
    public static async Task<AttendanceLimitExceeded?> Handle(
        AttendanceMarked @event, IQuerySession session)
    {
        var view = await session.Query<AttendanceView>()
            .FirstOrDefaultAsync(v => v.StudentId == @event.StudentId
                && v.BusinessId == @event.BusinessId);

        const int limit = 20;
        var total = (view?.UnexcusedDays ?? 0) + 1;

        if (total >= limit)
            return new AttendanceLimitExceeded(
                @event.StudentId, @event.InstitutionId, @event.BusinessId, total, limit);

        return null;
    }
}
