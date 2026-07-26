namespace MESNET.Enrollment.Shared.Events;

/// <summary>
/// Geçiş dolgusu (backfill) olayı (#119): mevcut fiilî yerleştirmelere göre bir işletmede
/// hangi alanlardan öğrenci bulunduğunu bildirir.
///
/// Business modülü bu olayı tüketip EKSİK alan yetkilerini üretir — hiçbir yetkiyi iptal etmez.
/// Kaynak veri Enrollment'ın kendi şemasındadır; Business başka modülün şemasını sorgulayamaz,
/// bu yüzden dolgu Enrollment'ta hesaplanıp olayla taşınır.
/// </summary>
public sealed record BusinessBranchUsageObserved(
    Guid BusinessId,
    List<string> BranchCodes,
    DateTime ObservedAt);
