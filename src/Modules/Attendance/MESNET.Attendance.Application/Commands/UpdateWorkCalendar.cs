namespace MESNET.Attendance.Application.Commands;

/// <remarks>
/// İşlemi yapan kullanıcı komutta TAŞINMAZ (#137) — handler token'dan
/// (<c>ICurrentUserService.GetUserId()</c>) damgalar.
/// </remarks>
public sealed record UpdateWorkCalendar(
    Guid InstitutionId,
    int Year,
    List<CalendarDayInput> RestrictedDays);
