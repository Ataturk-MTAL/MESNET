using JasperFx;

namespace MESNET.Attendance.Application.Commands;

public sealed record DeleteAttendance(
    [property: Identity] Guid AttendanceId);
