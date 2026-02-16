namespace MESNET.Institution.Application.Commands;

/// <summary>
/// Kurum ders programı ayarlarını günceller (günlük ders sayısı).
/// </summary>
public sealed record UpdateScheduleConfiguration(
    Guid InstitutionId,
    int DailyPeriodCount,  // 1-12 arası (validation ile kontrol edilecek)
    string UpdatedBy);
