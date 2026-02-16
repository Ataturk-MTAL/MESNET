namespace MESNET.Business.Shared.Events;

/// <summary>
/// Usta öğretici belgesi koordinatör tarafından geçersiz kılındığında tetiklenir.
/// </summary>
public sealed record InstructorDocumentInvalidated(
    Guid BusinessId,
    Guid DocumentId,
    string InvalidatedBy,
    string Reason,
    DateTime InvalidatedAt);
