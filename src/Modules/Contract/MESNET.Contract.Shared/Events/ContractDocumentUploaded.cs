namespace MESNET.Contract.Shared.Events;

/// <summary>
/// Sözleşmeye evrak yüklendiğinde tetiklenir.
/// </summary>
public sealed record ContractDocumentUploaded(
    Guid ContractId,
    Guid DocumentId,
    string DocumentType,
    string DocumentTypeSlug,
    string? Description,
    string ObjectPath,
    string UploadedBy,
    DateTime UploadedAt);
