namespace MESNET.Coordination.Application.Commands;

/// <summary>
/// Öğretmen ders programı oluştur/güncelle
///
/// <para>İşlemi yapan kullanıcı komutta TAŞINMAZ (#137) — handler token'dan damgalar ve
/// <c>ScheduleCreated</c>/<c>ScheduleUpdated</c> olayına o kimlik yazılır.</para>
/// </summary>
public sealed record UpsertTeacherSchedule(
    Guid TeacherId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    int AcademicYear,
    string Semester,  // "Fall" | "Spring"
    List<DailyScheduleInput> WeeklySchedule);

public sealed record DailyScheduleInput(
    string Day,  // "Monday", "Tuesday", "Wednesday", "Thursday", "Friday"
    List<PeriodSlotInput> Periods);

public sealed record PeriodSlotInput(
    int PeriodNumber,     // 1, 2, 3, ...
    string Status,        // "Occupied" | "Free"
    string? CourseName);  // Opsiyonel
