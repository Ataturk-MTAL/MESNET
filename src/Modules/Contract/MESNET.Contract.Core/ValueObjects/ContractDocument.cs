namespace MESNET.Contract.Core.ValueObjects;

public sealed record ContractDocument(
    Guid DocumentId,
    string DocumentType,      // ContractDocumentType.Name
    string DocumentTypeSlug,  // ContractDocumentType.Slug
    string? Description,
    string ObjectPath,
    string UploadedBy,
    DateTime UploadedAt);
