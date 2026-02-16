namespace MESNET.Attendance.Application.Commands;

public sealed record UpdateWorkCalendar(
    Guid InstitutionId,
    int Year,
    List<CalendarDayInput> RestrictedDays,
    string UpdatedBy);
