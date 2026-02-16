using MESNET.Attendance.Core.Enums;

namespace MESNET.Attendance.Core.ValueObjects;

public sealed record CalendarDay(DateTime Date, CalendarDayType Type, string Description);
