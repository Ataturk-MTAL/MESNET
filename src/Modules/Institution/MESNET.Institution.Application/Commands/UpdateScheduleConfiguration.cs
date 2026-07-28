namespace MESNET.Institution.Application.Commands;

/// <summary>
/// Kurum ders programı ayarlarını günceller (günlük ders sayısı).
///
/// <para>İşlemi yapan kullanıcı komutta TAŞINMAZ (#137) — handler token'dan
/// (<c>ICurrentUserService.GetUserId()</c>) damgalar.</para>
/// </summary>
public sealed record UpdateScheduleConfiguration(
    Guid InstitutionId,
    int DailyPeriodCount);  // 1-12 arası (validation ile kontrol edilecek)
