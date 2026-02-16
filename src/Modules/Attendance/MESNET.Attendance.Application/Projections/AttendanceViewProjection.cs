using Marten.Events.Projections;
using MESNET.Attendance.Core.Entities;
using MESNET.Attendance.Shared.Events;

namespace MESNET.Attendance.Application.Projections;

public class AttendanceViewProjection : MultiStreamProjection<AttendanceView, Guid>
{
    public AttendanceViewProjection()
    {
        Identity<AttendanceMarked>(e => e.StudentId);
        Identity<AttendanceLimitExceeded>(e => e.StudentId);
    }

    public static AttendanceView Create(AttendanceMarked e)
    {
        var view = new AttendanceView
        {
            Id = e.StudentId,
            StudentId = e.StudentId,
            BusinessId = e.BusinessId,
            LastUpdated = DateTime.UtcNow
        };

        ApplyAbsenceType(view, e.AbsenceType);
        return view;
    }

    public static void Apply(AttendanceMarked e, AttendanceView view)
    {
        ApplyAbsenceType(view, e.AbsenceType);
        view.LastUpdated = DateTime.UtcNow;
    }

    public static void Apply(AttendanceLimitExceeded e, AttendanceView view)
    {
        view.LimitExceeded = true;
        view.LastUpdated = DateTime.UtcNow;
    }

    private static void ApplyAbsenceType(AttendanceView view, string absenceType)
    {
        view.TotalAbsenceDays++;

        if (absenceType.Equals("Unexcused", StringComparison.OrdinalIgnoreCase))
            view.UnexcusedDays++;
        else
            view.ExcusedDays++;
    }
}
