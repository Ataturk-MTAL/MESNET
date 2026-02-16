namespace MESNET.Coordination.Application.Queries;

/// <summary>
/// Öğretmenin boş saatlerini getir
/// </summary>
public sealed record GetTeacherFreeSlots(
    Guid TeacherId,
    int AcademicYear,
    string Semester,
    string? Day);  // Opsiyonel: Sadece bir günü filtrele
