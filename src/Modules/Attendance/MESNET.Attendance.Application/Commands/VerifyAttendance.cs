using Marten.Schema;

namespace MESNET.Attendance.Application.Commands;

public sealed record VerifyAttendance([property: Identity] Guid AttendanceId);
