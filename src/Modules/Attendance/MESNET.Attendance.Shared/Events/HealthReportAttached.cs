namespace MESNET.Attendance.Shared.Events;

public sealed record HealthReportAttached(
    Guid AttendanceId,
    Guid StudentId,
    string ReportUrl,
    DateTime AttachedAt);
