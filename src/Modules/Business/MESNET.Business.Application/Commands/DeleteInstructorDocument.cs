namespace MESNET.Business.Application.Commands;

/// <summary>
/// Usta öğretici belgesini sil (koordinatör/müdür yardımcısı).
/// </summary>
public sealed record DeleteInstructorDocument(
    Guid BusinessId,
    Guid DocumentId,
    string DeletedBy,
    string Reason);
