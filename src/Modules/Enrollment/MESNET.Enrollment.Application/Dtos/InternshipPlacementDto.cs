namespace MESNET.Enrollment.Application.Dtos;

public sealed record InternshipPlacementDto(
    Guid Id,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid? TeacherId,
    string Status,
    string StatusSlug,
    string Source,
    string SourceSlug,
    DateTime PlacedAt,
    DateTime? TransferredAt,
    string? TransferReason);
