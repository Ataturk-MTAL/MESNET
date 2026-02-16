namespace MESNET.Attendance.Application.Commands;

public sealed record CalendarDayInput(DateTime Date, string Type, string Description);
