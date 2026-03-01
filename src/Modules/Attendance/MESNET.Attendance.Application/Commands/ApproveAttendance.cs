using Marten.Schema;

namespace MESNET.Attendance.Application.Commands;

public sealed record ApproveAttendance([property: Identity] Guid AttendanceId);
