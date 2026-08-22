using MESNET.Coordination.Core.Services;

namespace MESNET.Coordination.UnitTests.Services;

/// <summary>
/// Testlerde tekrar eden girdi kurulum yardımcıları.
/// </summary>
internal static class AllocationTestData
{
    internal const string DefaultBranch = "BIL";

    /// <summary>Deterministik, okunabilir test Guid'i üretir (00000000-...-00000000000N).</summary>
    internal static Guid Id(int n) => new($"00000000-0000-0000-0000-{n:D12}");

    internal static AllocationInput Business(
        int id,
        int maxHours,
        int studentCount,
        string branchCode = DefaultBranch) =>
        new(Id(id), branchCode, maxHours, studentCount, IsPinned: false, PinnedHours: 0);

    internal static AllocationInput Pinned(
        int id,
        int maxHours,
        int studentCount,
        int pinnedHours,
        string branchCode = DefaultBranch) =>
        new(Id(id), branchCode, maxHours, studentCount, IsPinned: true, PinnedHours: pinnedHours);

    /// <summary>Sonuçtan işletme başına önerilen saati okur.</summary>
    internal static int HoursOf(this AllocationResult result, int id) =>
        result.Lines.Single(l => l.BusinessId == Id(id)).SuggestedHours;

    internal static AllocationLine LineOf(this AllocationResult result, int id) =>
        result.Lines.Single(l => l.BusinessId == Id(id));

    /// <summary>Kapasitesi bol (kova ayrımını devre dışı bırakan) varsayılan öğretmen kapasitesi.</summary>
    internal const int UnlimitedCapacity = 1000;
}
