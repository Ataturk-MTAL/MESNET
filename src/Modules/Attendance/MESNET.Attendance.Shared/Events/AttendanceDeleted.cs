namespace MESNET.Attendance.Shared.Events;

public sealed record AttendanceDeleted(
    Guid AttendanceId,
    Guid StudentId,
    string DeletedBy,
    DateTime DeletedAt);
