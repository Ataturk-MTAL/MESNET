using Marten;
using MESNET.Coordination.Core.ReadModels;

namespace MESNET.Coordination.Application.Helpers;

/// <summary>
/// Alan bazlı koordinasyon satırını bulur (#114).
///
/// Birincil yol deterministik kimliktir (<see cref="CoordinationViewId.For"/>). Kimlik
/// tutmazsa — istemci alan/dönem göndermemişse veya kayıt çok-alanlı modele geçmeden önce
/// yazılmışsa — işletmenin satırları taranır ve <b>tek</b> aday varsa o döner. Birden çok alan
/// satırı varken alan kodu verilmemişse bilinçli olarak <c>null</c> döner: yanlış alanın
/// satırını güncellemektense hata vermek doğrudur.
/// </summary>
public static class CoordinationViewLookup
{
    public static async Task<BusinessCoordinationView?> LoadBranchRowAsync(
        IQuerySession session,
        Guid businessId,
        string? branchCode,
        Guid academicPeriodId,
        CancellationToken cancellationToken)
    {
        var row = await session.LoadAsync<BusinessCoordinationView>(
            CoordinationViewId.For(businessId, branchCode, academicPeriodId), cancellationToken);

        if (row is not null) return row;

        var candidates = await LoadAllRowsAsync(session, businessId, cancellationToken);

        var branchRows = candidates
            .Where(v => !string.IsNullOrWhiteSpace(v.BranchCode))
            .ToList();

        if (!string.IsNullOrWhiteSpace(branchCode))
        {
            branchRows = branchRows
                .Where(v => string.Equals(v.BranchCode, branchCode, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (academicPeriodId != Guid.Empty)
        {
            var periodRows = branchRows.Where(v => v.AcademicPeriodId == academicPeriodId).ToList();
            if (periodRows.Count > 0) branchRows = periodRows;
        }

        return branchRows.Count == 1 ? branchRows[0] : null;
    }

    /// <summary>
    /// İşletmeye ait tüm satırlar (temel satır + alan satırları).
    /// <c>Id == businessId</c> koşulu eski tek-satır kayıtlarını da kapsar.
    /// </summary>
    public static async Task<List<BusinessCoordinationView>> LoadAllRowsAsync(
        IQuerySession session,
        Guid businessId,
        CancellationToken cancellationToken)
    {
        var rows = await session.Query<BusinessCoordinationView>()
            .Where(v => v.BusinessId == businessId || v.Id == businessId)
            .ToListAsync(cancellationToken);

        return [.. rows];
    }
}
