namespace MESNET.Attendance.Application.Dtos;

public sealed record AttendanceViewDto(
    Guid Id,
    Guid StudentId,
    Guid BusinessId,
    int TotalAbsenceDays,
    int ExcusedDays,
    int UnexcusedDays,
    bool LimitExceeded,
    DateTime LastUpdated);
