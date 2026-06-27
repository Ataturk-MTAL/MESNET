using JasperFx;
using MESNET.Attendance.Application.Guards;

namespace MESNET.Attendance.Application.Commands;

public sealed record ApproveAttendance([property: Identity] Guid AttendanceId) : IAttendancePeriodScoped;
