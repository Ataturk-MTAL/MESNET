namespace MESNET.Attendance.Application.Dtos;

public sealed record AttendanceRecordDto(
    Guid Id,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    DateTime Date,
    string AbsenceType,
    string AbsenceTypeSlug,
    string? Reason,
    string Status,
    string StatusSlug,
    string? HealthReportUrl,
    string MarkedBy,
    DateTime MarkedAt,
    string? VerifiedBy,
    DateTime? VerifiedAt);
