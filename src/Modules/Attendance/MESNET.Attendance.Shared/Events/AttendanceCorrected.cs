namespace MESNET.Attendance.Shared.Events;

public sealed record AttendanceCorrected(
    Guid AttendanceId,
    Guid StudentId,
    string CorrectedBy,
    string NewAbsenceType,
    string Reason,
    DateTime CorrectedAt);
