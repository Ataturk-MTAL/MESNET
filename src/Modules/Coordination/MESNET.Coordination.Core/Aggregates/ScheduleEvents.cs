namespace MESNET.Coordination.Core.Aggregates;

/// <summary>
/// Ders programı ilk oluşturuldu
/// </summary>
public sealed record ScheduleCreated(
    Guid ScheduleId,
    Guid TeacherId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    int AcademicYear,
    string Semester,
    List<DailyScheduleData> WeeklySchedule,
    string UpdatedBy,
    DateTime Timestamp);

/// <summary>
/// Ders programı güncellendi (yeni versiyon)
/// Her güncelleme bir event olarak kaydedilir — geri doğru takip edilebilir.
/// Geçerli program = son ScheduleUpdated event'inden oluşan state.
/// </summary>
public sealed record ScheduleUpdated(
    Guid ScheduleId,
    List<DailyScheduleData> WeeklySchedule,
    string UpdatedBy,
    DateTime Timestamp);
