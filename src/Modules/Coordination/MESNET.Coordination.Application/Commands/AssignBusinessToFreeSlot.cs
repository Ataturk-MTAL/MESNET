namespace MESNET.Coordination.Application.Commands;

/// <summary>
/// Öğretmenin boş saatine işletme ata (koordinasyon görevi)
/// </summary>
public sealed record AssignBusinessToFreeSlot(
    Guid TeacherId,
    int AcademicYear,
    string Semester,
    string Day,          // "Monday", ...
    int PeriodNumber,
    Guid BusinessId,
    string AssignedBy);
