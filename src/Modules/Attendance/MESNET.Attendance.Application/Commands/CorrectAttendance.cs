using JasperFx;
using MESNET.Attendance.Application.Guards;

namespace MESNET.Attendance.Application.Commands;

public sealed record CorrectAttendance(
    [property: Identity] Guid AttendanceId,
    string NewAbsenceType,
    string Reason) : IAttendancePeriodScoped;
