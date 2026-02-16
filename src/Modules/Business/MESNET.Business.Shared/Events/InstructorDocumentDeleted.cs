namespace MESNET.Business.Shared.Events;

/// <summary>
/// Usta öğretici belgesi koordinatör/müdür yardımcısı tarafından silind iğinde tetiklenir.
/// </summary>
public sealed record InstructorDocumentDeleted(
    Guid BusinessId,
    Guid DocumentId,
    string DeletedBy,
    string Reason,
    DateTime DeletedAt);
