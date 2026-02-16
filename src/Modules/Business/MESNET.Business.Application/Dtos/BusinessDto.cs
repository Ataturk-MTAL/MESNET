using MESNET.Common.Shared;

namespace MESNET.Business.Application.Dtos;

public sealed record BusinessDto(
    Guid Id,
    string Name,
    string Address,
    string? PhoneNumber,
    string? Email,
    string? Website,
    string Status,
    string StatusSlug,
    string Source,
    string SourceSlug,
    int PersonnelCount,
    Location? Location,
    BusinessCapacityDto Capacity,
    List<BusinessRepresentativeDto> Representatives,
    List<BusinessDocumentDto> Documents,
    DateTime CreatedAt,
    DateTime? ApprovedAt,
    DateTime? ClosedAt);

public sealed record BusinessCapacityDto(
    int TotalSlots,
    int OccupiedSlots,
    int AvailableSlots,
    bool IsFull);

public sealed record BusinessRepresentativeDto(
    Guid Id,
    string FullName,
    string PhoneNumber,
    string Email,
    DateTime AuthorizedAt);

public sealed record BusinessDocumentDto(
    Guid Id,
    string Type,
    string TypeSlug,
    string Status,
    string StatusSlug,
    string FileName,
    DateTime UploadedAt,
    DateTime? ApprovedAt,
    DateTime? ExpiresAt,
    string? RejectionReason);
