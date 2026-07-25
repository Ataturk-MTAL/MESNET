namespace MESNET.Enrollment.Application.Commands;

/// <summary>
/// Sonlanmamış tüm yerleştirmeler için <c>StudentPlaced</c> olayını yeniden yayınlar — diğer
/// modüllerin denormalize yerleştirme read-model'lerini tazeler.
/// </summary>
/// <remarks>
/// <c>ResyncStudentProjections</c> yalnız <c>StudentRegistered</c> yayınlıyor; yerleştirme
/// karşılığı yoktu. Yeni bir yerleştirme read-model'i eklendiğinde (ör. Payment'ın
/// <c>PlacementView</c>'ı, #63) mevcut kayıtlar için geriye dönük dolmuyordu ve aylık maaş
/// zamanlayıcısı sıfır yerleştirme buluyordu (#77).
///
/// Yalnız <c>IsFinal</c> olmayan yerleştirmeler yayınlanır — tamamlanmış/fesihli kayıtların
/// yeniden aktif işaretlenmesi yanlış olurdu.
///
/// Tüm consumer'lar idempotent upsert (<c>session.Store</c>) yaptığından tekrar çalıştırmak güvenlidir.
/// </remarks>
public sealed record ResyncPlacementProjections;

public sealed record ResyncPlacementProjectionsResult(int PlacementCount, int Skipped);
