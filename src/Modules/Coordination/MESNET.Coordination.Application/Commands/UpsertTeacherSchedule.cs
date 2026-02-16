namespace MESNET.Coordination.Application.Commands;

/// <summary>
/// Öğretmen ders programı oluştur/güncelle
/// </summary>
public sealed record UpsertTeacherSchedule(
    Guid TeacherId,
    Guid InstitutionId,
    int AcademicYear,
    string Semester,  // "Fall" | "Spring"
    List<DailyScheduleInput> WeeklySchedule,
    string UpdatedBy);

public sealed record DailyScheduleInput(
    string Day,  // "Monday", "Tuesday", "Wednesday", "Thursday", "Friday"
    List<PeriodSlotInput> Periods);

public sealed record PeriodSlotInput(
    int PeriodNumber,     // 1, 2, 3, ...
    string Status,        // "Occupied" | "Free"
    string? CourseName);  // Opsiyonel
