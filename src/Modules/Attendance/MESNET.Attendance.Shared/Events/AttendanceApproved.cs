namespace MESNET.Attendance.Shared.Events;

public sealed record AttendanceApproved(
    Guid AttendanceId,
    Guid StudentId,
    string ApprovedBy,
    DateTime ApprovedAt);
