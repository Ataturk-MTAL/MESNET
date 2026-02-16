namespace MESNET.Attendance.Application.Commands;

public sealed record VerifyAttendance(Guid AttendanceId, string VerifiedBy);
