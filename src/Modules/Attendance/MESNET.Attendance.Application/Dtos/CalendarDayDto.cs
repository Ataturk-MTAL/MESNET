namespace MESNET.Attendance.Application.Dtos;

public sealed record CalendarDayDto(
    DateTime Date,
    string Type,
    string TypeSlug,
    string Description);
