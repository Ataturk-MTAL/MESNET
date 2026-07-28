namespace MESNET.Attendance.Shared.Events;

/// <param name="UpdatedById">
/// İşlemi yapan kullanıcının kimliği — token'dan gelir, istekten ALINMAZ (#137).
/// Modüller arası olayda ad taşınmaz; her modül adı kendi <c>UserNameView</c>'ından çözer.
/// </param>
public sealed record WorkCalendarUpdated(
    Guid CalendarId,
    Guid InstitutionId,
    int Year,
    int RestrictedDayCount,
    Guid UpdatedById,
    List<CalendarDayInfo>? RestrictedDays = null);

/// <summary>
/// Takvim günü bilgisi — modüller arası event taşıma için.
/// </summary>
public sealed record CalendarDayInfo(DateTime Date, string Type, string Description);
