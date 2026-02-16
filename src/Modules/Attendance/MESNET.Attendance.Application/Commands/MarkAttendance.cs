namespace MESNET.Attendance.Application.Commands;

public sealed record MarkAttendance(
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    DateTime Date,
    string AbsenceType,
    string? Reason,
    string MarkedBy);
