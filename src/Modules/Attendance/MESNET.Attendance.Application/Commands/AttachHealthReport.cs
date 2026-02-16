namespace MESNET.Attendance.Application.Commands;

public sealed record AttachHealthReport(Guid AttendanceId, string ReportUrl);
