namespace MESNET.Contract.Application.Dtos;

public sealed record InternshipContractDto(
    Guid Id,
    Guid StudentId,
    Guid BusinessId,
    Guid InstitutionId,
    Guid? TeacherId,
    string Status,
    string StatusSlug,
    DateTime StartDate,
    DateTime? EndDate,
    SignatureStatusDto InstitutionSignature,
    SignatureStatusDto BusinessSignature,
    SignatureStatusDto StudentSignature,
    string? TerminationReason,
    string? TerminationReasonType,
    string? TerminationReasonTypeSlug,
    IReadOnlyList<ContractDocumentDto> Documents,
    DateTime CreatedAt);

public sealed record SignatureStatusDto(bool IsSigned, string? SignedBy, DateTime? SignedAt);

public sealed record ContractDocumentDto(
    Guid DocumentId,
    string DocumentType,
    string DocumentTypeSlug,
    string? Description,
    string ObjectPath,
    string UploadedBy,
    DateTime UploadedAt);
