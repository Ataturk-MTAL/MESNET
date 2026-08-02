namespace MESNET.Attendance.Application.Dtos;

/// <summary>Ücretli izin başvurusu okuma modeli (#177).</summary>
public sealed record PaidLeaveRequestDto(
    Guid Id,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    DateTime StartDate,
    DateTime EndDate,
    int DayCount,
    string Reason,
    string Status,
    string StatusSlug,
    Guid RequestedById,
    string? RequestedByName,
    DateTime RequestedAt,
    Guid? BusinessApprovedById,
    string? BusinessApprovedByName,
    DateTime? BusinessApprovedAt,
    Guid? ApprovedById,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    Guid? RejectedById,
    string? RejectedByName,
    DateTime? RejectedAt,
    string? RejectionReason);
