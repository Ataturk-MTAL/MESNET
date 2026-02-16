namespace MESNET.Business.Shared.Events;

/// <summary>
/// Usta öğretici belgesi yüklendiğinde tetiklenir.
/// </summary>
public sealed record InstructorDocumentUploaded(
    Guid BusinessId,
    Guid DocumentId,
    string ObjectPath,
    string UploadedBy,
    DateTime UploadedAt,
    DateTime? ExpiresAt);
