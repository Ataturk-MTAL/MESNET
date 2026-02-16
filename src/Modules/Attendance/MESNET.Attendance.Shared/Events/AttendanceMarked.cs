namespace MESNET.Attendance.Shared.Events;

public sealed record AttendanceMarked(
    Guid AttendanceId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    DateTime Date,
    string AbsenceType);
