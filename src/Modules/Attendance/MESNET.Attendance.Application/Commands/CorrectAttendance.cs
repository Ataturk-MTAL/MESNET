namespace MESNET.Attendance.Application.Commands;

public sealed record CorrectAttendance(
    Guid AttendanceId,
    string NewAbsenceType,
    string Reason,
    string CorrectedBy);
