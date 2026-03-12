namespace MESNET.Attendance.Shared.Events;

public sealed record WorkCalendarUpdated(
    Guid CalendarId,
    Guid InstitutionId,
    int Year,
    int RestrictedDayCount,
    string UpdatedBy,
    List<CalendarDayInfo>? RestrictedDays = null);

/// <summary>
/// Takvim günü bilgisi — modüller arası event taşıma için.
/// </summary>
public sealed record CalendarDayInfo(DateTime Date, string Type, string Description);
