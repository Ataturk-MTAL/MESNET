using Marten.Schema;

namespace MESNET.Attendance.Application.Commands;

public sealed record DeleteAttendance(
    [property: Identity] Guid AttendanceId);
