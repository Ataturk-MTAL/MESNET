namespace MESNET.Enrollment.Application.Queries;

/// <summary>
/// Yerleştirmelerin durum-bazında TOPLAM sayımı (sayfalamadan bağımsız) — overview kartları için.
/// Yetki-kapsamı liste ile aynıdır (kurum + rol); status filtresi UYGULANMAZ — her durumun sayısı gerekir.
/// </summary>
public sealed record GetPlacementStatusCounts(Guid? AcademicPeriodId, string? BranchCode);

/// <summary>Durum adı (StatusName) → adet. Eşleşme yoksa ilgili anahtar bulunmaz.</summary>
public sealed record PlacementStatusCountsResult(Dictionary<string, int> Counts);
