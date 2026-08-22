namespace MESNET.Attendance.Application.Dtos;

public sealed record WorkCalendarDto(
    Guid Id,
    Guid InstitutionId,
    int Year,
    List<CalendarDayDto> RestrictedDays,
    // Kimlik saklanır, ad okuma anında UserNameView'dan çözülür (#137).
    Guid UpdatedById,
    string? UpdatedByName,
    DateTime UpdatedAt);
