using MESNET.Enrollment.Core.Entities;
using MESNET.Enrollment.Core.ReadModels;

namespace MESNET.Enrollment.Core.Policies;

/// <summary>
/// Yerleştirmenin alan yetkisi kuralları (#119) — saf (yan etkisiz) fonksiyonlar.
/// Handler yalnız yükleme/kaydetme yapar; karar burada test edilebilir biçimde durur.
/// </summary>
public static class PlacementBranchPolicy
{
    /// <summary>
    /// Öğrencinin alanı, işletmenin AKTİF yetkili alanları arasında mı?
    /// Yetki kaydı hiç yoksa (view null) veya liste boşsa cevap HAYIR — boş liste KAPALI demektir.
    /// </summary>
    public static bool IsBusinessAuthorizedFor(BusinessBranchAuthorizationView? view, string? branchCode)
    {
        if (view is null) return false;
        if (string.IsNullOrWhiteSpace(branchCode)) return false;

        var needle = branchCode.Trim();
        return view.ActiveBranchCodes.Any(c => string.Equals(c, needle, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Geçiş dolgusu (backfill) girdisi: fiilî yerleştirmelerden işletme başına farklı alan kodları.
    /// Boş alan kodu taşıyan yerleştirmeler yok sayılır.
    /// </summary>
    public static Dictionary<Guid, List<string>> GroupBranchCodesByBusiness(
        IEnumerable<InternshipPlacement> placements) =>
        placements
            .Where(p => p.BusinessId != Guid.Empty && !string.IsNullOrWhiteSpace(p.BranchCode))
            .GroupBy(p => p.BusinessId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(p => p.BranchCode.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                    .ToList());
}
