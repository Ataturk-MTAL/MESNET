namespace MESNET.Attendance.Shared.Events;

public sealed record AttendanceVerified(
    Guid AttendanceId,
    Guid StudentId,
    string VerifiedBy,
    DateTime VerifiedAt);
