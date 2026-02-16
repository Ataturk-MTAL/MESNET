namespace MESNET.Business.Application.Commands;

/// <summary>
/// Usta öğretici belgesi talep et (koordinatör işletmeye yeniden yükleme talebi gönderir).
/// </summary>
public sealed record RequestInstructorDocument(
    Guid BusinessId,
    string RequestedBy,
    string Reason,
    DateTime Deadline);
