namespace MESNET.Attendance.Application.Dtos;

public sealed record WorkCalendarDto(
    Guid Id,
    Guid InstitutionId,
    int Year,
    List<CalendarDayDto> RestrictedDays,
    string UpdatedBy,
    DateTime UpdatedAt);
