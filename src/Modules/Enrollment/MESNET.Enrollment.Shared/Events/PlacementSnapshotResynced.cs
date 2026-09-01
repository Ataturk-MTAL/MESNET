namespace MESNET.Enrollment.Shared.Events;

/// <summary>
/// Bir yerleştirmenin <b>o anki tam hâli</b> — diğer modüllerin yerel görünümlerini geçmişe
/// dönük onarmak için (#291).
///
/// <para><b>Neden ayrı bir olay, <c>StudentPlaced</c>'i yeniden yayınlamak yerine:</b>
/// <c>StudentPlaced</c> staj saga'sının <b>başlatıcı</b> olayıdır
/// (<c>InternshipSaga.Start</c>). Yeniden yayınlandığında Wolverine, deterministik kimlikli
/// (#251) saga'yı <b>yeniden INSERT etmeye</b> çalışır ve tekil kısıt ihlaliyle o kuyruk ölü
/// mektuba düşer. <c>MultipleHandlerBehavior.Separated</c> yüzünden kardeş kuyruklar commit
/// etmeye devam eder: uç <b>200 döner</b>, saga yazılmaz, kapasite bozulur. Onarım amaçlı bir
/// yeniden yayının yaşam döngüsü başlatması kabul edilemez — <c>AttendanceSnapshotResynced</c>
/// ile aynı gerekçe (#256).</para>
///
/// <para><b>Bu olayı saga TÜKETMEZ.</b> Tüketicisi yalnız görünüm besleyen taraftır. Saga'yı
/// onaran ayrı bir yol vardır: <c>POST /api/internships/resync-sagas</c>.</para>
///
/// <para><b>Neden alan listesi <c>StudentPlaced</c> ile aynı:</b> tüketiciler ad alanlarını
/// (<c>StudentName</c>, <c>BusinessName</c>, <c>BranchName</c>) denormalize tutuyor. Eksik
/// yayınlamak onların verisini boş dizeyle ezerdi — onarım adına veri kaybı.</para>
/// </summary>
public sealed record PlacementSnapshotResynced(
    Guid PlacementId,
    Guid StudentId,
    Guid? BusinessId,
    Guid InstitutionId,
    Guid AcademicPeriodId,
    Guid? TeacherId,
    DateTime PlacedAt,
    string StudentName,
    string BusinessName,
    string BranchCode,
    string BranchName,
    string PlacementType);
