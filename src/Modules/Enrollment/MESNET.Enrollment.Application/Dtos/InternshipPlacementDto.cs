namespace MESNET.Enrollment.Application.Dtos;

public sealed record InternshipPlacementDto(
    Guid Id,
    Guid StudentId,
    string StudentName,
    Guid BusinessId,
    string BusinessName,
    Guid InstitutionId,
    Guid? TeacherId,
    string? TeacherName,
    string Status,
    string StatusSlug,
    string Source,
    string SourceSlug,
    DateTime PlacedAt,
    DateTime? TransferredAt,
    string? TransferReason);
