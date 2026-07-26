namespace MESNET.Coordination.Application.Commands;

/// <summary>
/// Koordinasyon işletme satırlarını çok-alanlı modele göre yeniden kurar (#114).
///
/// Marten JSONB olduğu için tablo migration'ı yoktur; ancak eski tek-satır kayıtları
/// (<c>Id = BusinessId</c>) yeni deterministik kimlik şemasında öksüz kalır. Bu komut
/// eski satırları siler, işletme düzeyi temel satırı ve her <c>(alan, dönem)</c> için
/// ayrı satırı yeniden üretir. Mevcut öğretmen ataması / takdir saati / geçmiş,
/// alan ve dönemi eşleşen eski satırdan taşınır.
/// </summary>
public sealed record ResyncCoordinationViews(Guid InstitutionId);

public sealed record ResyncCoordinationViewsResult(
    int BaseRows,
    int BranchRows,
    int RemovedLegacyRows);
