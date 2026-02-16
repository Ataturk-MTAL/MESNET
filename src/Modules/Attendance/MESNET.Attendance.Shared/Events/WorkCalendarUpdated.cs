namespace MESNET.Attendance.Shared.Events;

public sealed record WorkCalendarUpdated(
    Guid CalendarId,
    Guid InstitutionId,
    int Year,
    int RestrictedDayCount,
    string UpdatedBy);
