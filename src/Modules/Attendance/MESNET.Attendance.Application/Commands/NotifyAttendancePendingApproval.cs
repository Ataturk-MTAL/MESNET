namespace MESNET.Attendance.Application.Commands;

public sealed record NotifyAttendancePendingApproval(
    Guid AttendanceId,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid CoordinatorTeacherId,
    string MarkedByName,
    DateTime Date,
    string AbsenceType);
