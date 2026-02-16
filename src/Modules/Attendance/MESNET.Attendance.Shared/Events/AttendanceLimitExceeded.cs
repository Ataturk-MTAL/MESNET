namespace MESNET.Attendance.Shared.Events;

public sealed record AttendanceLimitExceeded(
    Guid StudentId,
    Guid InstitutionId,
    Guid BusinessId,
    int TotalAbsenceDays,
    int Limit);
