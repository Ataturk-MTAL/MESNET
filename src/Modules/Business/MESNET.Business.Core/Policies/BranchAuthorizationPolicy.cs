using MESNET.Business.Core.ValueObjects;

namespace MESNET.Business.Core.Policies;

/// <summary>İdarenin işaretlediği tek bir alan + dayanak belgesi.</summary>
public sealed record BranchAuthorizationRequest(string BranchCode, Guid? BasedOnDocumentId = null);

/// <summary>
/// Alan yetkisi listesinin saf (yan etkisiz) kuralları (#119). Handler'lar yalnız yükleme /
/// kaydetme yapar; karar mantığı burada test edilebilir biçimde durur.
/// </summary>
public static class BranchAuthorizationPolicy
{
    /// <summary>Boş yetki listesi = KAPALI. Hiçbir alandan öğrenci alınamaz.</summary>
    public static List<string> ActiveCodes(IEnumerable<BranchAuthorization> authorizations) =>
        authorizations
            .Where(a => a.IsActive)
            .Select(a => a.BranchCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static bool IsAuthorizedFor(IEnumerable<BranchAuthorization> authorizations, string? branchCode)
    {
        if (string.IsNullOrWhiteSpace(branchCode)) return false;
        var needle = branchCode.Trim();
        return authorizations.Any(a =>
            a.IsActive && string.Equals(a.BranchCode, needle, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// İdarenin işaretlediği listeyi uygular — YERİNE KOYMA semantiği:
    /// listedeki alanlar aktif yetkiye dönüşür, listede olmayan aktif yetkiler iptal edilir.
    /// İptal edilen kayıt silinmez, <c>RevokedAt</c> damgalanır (denetim izi).
    /// </summary>
    public static List<BranchAuthorization> Apply(
        IEnumerable<BranchAuthorization> current,
        IEnumerable<BranchAuthorizationRequest> requested,
        string authorizedBy,
        DateTime now)
    {
        var requestedByCode = requested
            .Where(r => !string.IsNullOrWhiteSpace(r.BranchCode))
            .GroupBy(r => r.BranchCode.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last(), StringComparer.OrdinalIgnoreCase);

        var result = new List<BranchAuthorization>();
        var kept = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var existing in current)
        {
            if (!existing.IsActive)
            {
                result.Add(existing);
                continue;
            }

            if (!requestedByCode.TryGetValue(existing.BranchCode, out var request))
            {
                result.Add(existing with { RevokedAt = now });
                continue;
            }

            // Dayanak belge değişmediyse kayda dokunma — gereksiz "yeniden yetkilendirildi" izi olmasın.
            result.Add(existing.BasedOnDocumentId == request.BasedOnDocumentId
                ? existing
                : existing with
                {
                    BasedOnDocumentId = request.BasedOnDocumentId,
                    AuthorizedAt = now,
                    AuthorizedBy = authorizedBy
                });
            kept.Add(existing.BranchCode);
        }

        foreach (var (code, request) in requestedByCode)
        {
            if (kept.Contains(code)) continue;
            result.Add(new BranchAuthorization
            {
                BranchCode = code,
                BasedOnDocumentId = request.BasedOnDocumentId,
                AuthorizedAt = now,
                AuthorizedBy = authorizedBy
            });
        }

        return result;
    }

    /// <summary>
    /// Geçiş dolgusu (backfill): yalnız EKSİK alanları ekler, hiçbir yetkiyi iptal etmez.
    /// Mevcut fiilî yerleştirmelerden türetilen yetkiler için kullanılır.
    /// </summary>
    public static List<BranchAuthorization> Merge(
        IEnumerable<BranchAuthorization> current,
        IEnumerable<string> branchCodes,
        string authorizedBy,
        DateTime now)
    {
        var result = current.ToList();

        var codes = branchCodes
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var code in codes)
        {
            if (IsAuthorizedFor(result, code)) continue;
            result.Add(new BranchAuthorization
            {
                BranchCode = code,
                AuthorizedAt = now,
                AuthorizedBy = authorizedBy
            });
        }

        return result;
    }
}
