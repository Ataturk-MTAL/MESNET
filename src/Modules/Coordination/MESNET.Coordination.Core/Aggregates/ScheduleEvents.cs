namespace MESNET.Coordination.Core.Aggregates;

/// <summary>
/// Ders programı ilk oluşturuldu
/// </summary>
/// <param name="UpdatedById">
/// İşlemi yapan kullanıcının kimliği — token'ın <c>sub</c> claim'inden gelir, istekten
/// ALINMAZ (#137). Ad, sorgu tarafında <c>UserNameView</c>'dan çözülür.
///
/// <para><b>Neden yeni ad, eski <c>UpdatedBy</c> alanının tipini değiştirmek yerine:</b>
/// bu event <c>shared.mt_events</c> içinde kalıcıdır. Aynı adı <c>Guid</c> yapmak, saklı
/// <c>"updatedBy": "admin"</c> değerini okunamaz kılar ve aggregate replay'i
/// <c>JsonException</c> ile tümden kırardı. Yeni ad eski anahtarı sessizce yok sayar;
/// eski, zaten güvenilmez olan ad kaybedilir — bilinçli kabul edilen kayıp.</para>
/// </param>
public sealed record ScheduleCreated(
    Guid ScheduleId,
    Guid TeacherId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    int AcademicYear,
    string Semester,
    List<DailyScheduleData> WeeklySchedule,
    Guid UpdatedById,
    DateTime Timestamp);

/// <summary>
/// Ders programı güncellendi (yeni versiyon)
/// Her güncelleme bir event olarak kaydedilir — geri doğru takip edilebilir.
/// Geçerli program = son ScheduleUpdated event'inden oluşan state.
/// </summary>
/// <param name="UpdatedById">Bkz. <see cref="ScheduleCreated.UpdatedById"/>.</param>
public sealed record ScheduleUpdated(
    Guid ScheduleId,
    List<DailyScheduleData> WeeklySchedule,
    Guid UpdatedById,
    DateTime Timestamp);
