namespace MESNET.Enrollment.Shared.Events;

/// <summary>
/// Bir öğrenci kaydının <b>o anki tam hâli</b> — diğer modüllerin yerel görünümlerini geçmişe
/// dönük onarmak için (#290).
///
/// <para><b>Neden ayrı bir olay, <c>StudentRegistered</c>'ı yeniden yayınlamak yerine:</b>
/// o olayın tüketicilerinden biri <b>sayaç artırıyor</b>
/// (<c>Coordination.StudentRegisteredCountConsumer</c>:
/// <c>StudentCountByClassYear[classYear] = current + 1</c>). Görünüm öğrenci başına değil
/// <b>şube başına</b> tek satırdır — <c>BranchStudentCountView.CreateId</c> imzasında
/// <c>studentId</c> yoktur. Yani her yeniden yayın, her şubenin sayacını o şubedeki öğrenci
/// sayısı kadar şişiriyordu; ikinci koşuda sayı ikiye katlanıyordu. Uç 200 dönüyor, log temiz
/// kalıyordu.</para>
///
/// <para><b>Sayacı bu olay TÜKETMEZ.</b> Onarımı ayrı ve <b>mutlak</b> bir yoldan gelir:
/// <c>SyncStudentCounts</c> sayacı artırmaz, <b>değiştirir</b>. Sayaç tüketicisine bu olayın
/// bir aşırı yüklemesini eklemek düzeltmeyi geri alır.</para>
///
/// <para><b>Neden yalnız <c>SyncStudentCounts</c> eklemek yetmezdi:</b>
/// <c>MultipleHandlerBehavior.Separated</c> her handler tipine ayrı kuyruk verir ve kuyruklar
/// arasında sıra garantisi <b>yoktur</b>. Yeniden yayın sürerken çalışan bir "değiştir" adımı,
/// arkasından gelen artırımlarla yine şişerdi. Artıranı hiç tetiklememek tek güvenli yoldur.</para>
///
/// <para><b>Alan listesi <c>StudentRegistered</c> ile aynı:</b> tüketiciler ad, şube ve dönem
/// alanlarını denormalize tutuyor; eksik yayınlamak onların verisini boş değerle ezerdi
/// (<c>PlacementSnapshotResynced</c> ile aynı gerekçe, #291).</para>
/// </summary>
public sealed record StudentSnapshotResynced(
    Guid StudentId,
    string FullName,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    string BranchCode,
    int ClassYear,
    string EducationType,
    string StudentNumber,
    bool HasJourneymanQualification,
    DateTime? BirthDate,
    string Category,
    Guid KeycloakUserId);
