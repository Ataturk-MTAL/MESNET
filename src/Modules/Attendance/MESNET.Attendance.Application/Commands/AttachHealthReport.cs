using JasperFx;
using MESNET.Attendance.Application.Guards;

namespace MESNET.Attendance.Application.Commands;

public sealed record AttachHealthReport([property: Identity] Guid AttendanceId, string ReportUrl) : IAttendancePeriodScoped;
