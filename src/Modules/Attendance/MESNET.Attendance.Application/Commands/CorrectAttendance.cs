using JasperFx;

namespace MESNET.Attendance.Application.Commands;

public sealed record CorrectAttendance(
    [property: Identity] Guid AttendanceId,
    string NewAbsenceType,
    string Reason);
