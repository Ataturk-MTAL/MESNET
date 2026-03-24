namespace MESNET.Enrollment.Application.Dtos;

public sealed record InternshipPlacementDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid BusinessId,
    string BusinessName,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    Guid? TeacherId,
    string? TeacherName,
    string BranchCode,
    string Status,
    string StatusSlug,
    string Source,
    string SourceSlug,
    DateTime PlacedAt);
