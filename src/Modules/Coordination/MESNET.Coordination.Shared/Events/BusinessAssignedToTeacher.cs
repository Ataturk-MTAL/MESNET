namespace MESNET.Coordination.Shared.Events;

/// <summary>
/// Öğretmenin boş saatine işletme atandı
/// Raporlama ve Internship (saga) modülleri bu eventi dinler
/// </summary>
public sealed record BusinessAssignedToTeacher(
    Guid ScheduleId,
    Guid TeacherId,
    Guid BusinessId,
    string Day,
    int PeriodNumber,
    int AcademicYear,
    string Semester);
