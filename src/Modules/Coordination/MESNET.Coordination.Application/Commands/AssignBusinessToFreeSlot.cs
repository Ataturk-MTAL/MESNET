namespace MESNET.Coordination.Application.Commands;

/// <summary>
/// Öğretmenin boş saatine işletme ata (koordinasyon görevi)
///
/// <para>Atamayı yapan kullanıcı komutta TAŞINMAZ (#137) — handler token'dan damgalar.</para>
/// </summary>
public sealed record AssignBusinessToFreeSlot(
    Guid TeacherId,
    int AcademicYear,
    string Semester,
    string Day,          // "Monday", ...
    int PeriodNumber,
    Guid BusinessId);
