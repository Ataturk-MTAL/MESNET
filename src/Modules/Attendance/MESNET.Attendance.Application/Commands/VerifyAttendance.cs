using JasperFx;
using MESNET.Attendance.Application.Guards;

namespace MESNET.Attendance.Application.Commands;

public sealed record VerifyAttendance([property: Identity] Guid AttendanceId) : IAttendancePeriodScoped;
