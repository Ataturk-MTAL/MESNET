namespace MESNET.Business.Application.Commands;

/// <summary>
/// Usta öğretici belgesini geçersiz kıl (koordinatör).
/// </summary>
public sealed record InvalidateInstructorDocument(
    Guid BusinessId,
    Guid DocumentId,
    string InvalidatedBy,
    string Reason);
