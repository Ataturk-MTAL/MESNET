namespace MESNET.Business.Shared.Events;

/// <summary>
/// Usta öğretici belgesi işletmeden talep edildiğinde tetiklenir.
/// </summary>
public sealed record InstructorDocumentRequested(
    Guid BusinessId,
    string RequestedBy,
    string Reason,
    DateTime RequestedAt,
    DateTime Deadline);
