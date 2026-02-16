namespace MESNET.Coordination.Shared.Events;

/// <summary>
/// Öğretmen ders programı oluşturuldu/güncellendi
/// Raporlama modülü bu eventi dinleyerek öğretmen müsaitlik raporlarını günceller
/// </summary>
public sealed record TeacherScheduleUpserted(
    Guid ScheduleId,
    Guid TeacherId,
    int AcademicYear,
    string Semester,
    bool IsNew);
