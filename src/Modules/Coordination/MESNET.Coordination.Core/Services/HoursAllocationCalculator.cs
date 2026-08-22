using MESNET.Coordination.Core.Enums;

namespace MESNET.Coordination.Core.Services;

/// <summary>
/// Ders yükü havuzunu (<c>P</c>) alanın işletmelerine dağıtan saf algoritma (issue #116).
/// Dış bağımlılığı yoktur; girdiyi değiştirmez, yeni bir sonuç üretir.
///
/// Ağırlık: <c>w_i = max_i × s_i</c> (uzak ve kalabalık işletme daha çok saat alır).
///
/// Adımlar:
/// 1. Sabitlenmiş satırların saatleri havuzdan düşülür ve hiç değiştirilmez.
/// 2. Kalan havuz <c>w_i</c> oranında dağıtılır, sonuç <c>[1, max_i]</c> aralığına kırpılır.
/// 3. Kırpmadan artan saatler doymamış işletmelere yeniden dağıtılır (su-doldurma turu).
/// 4. Tamsayı artığı en büyük ondalık kalana verilir (Hare-Niemeyer).
/// 5. Satırlar üç kovaya ayrılır: alan içi ücretli / alan dışı öneri / fahri.
///
/// Çözücü (OR-Tools/ILP) bilinçli olarak kullanılmadı: idari itiraza açık bir dağıtımın
/// "neden bu işletmeye 6 saat" sorusuna tek satırla (ağırlık sırası) cevap verebilmesi gerekir.
/// Karmaşıklık O(n log n + n·tur); n ≈ 100.
/// </summary>
public static class HoursAllocationCalculator
{
    /// <summary>Ücretli sayılan bir ziyaretin taban saati.</summary>
    private const int MinPaidHours = 1;

    /// <summary>
    /// Havuzu işletmelere dağıtır ve tanılamayla birlikte döndürür.
    /// </summary>
    /// <param name="businesses">Alanın işletme satırları. Değiştirilmez.</param>
    /// <param name="pool">Ders yükü havuzu (<c>P</c>). <c>0</c> veya altı → öneri üretilmez.</param>
    /// <param name="teacherCapacity">Alan öğretmeni boş saat kapasitesi (<c>C</c>).</param>
    /// <exception cref="ArgumentNullException"><paramref name="businesses"/> null ise.</exception>
    public static AllocationResult Allocate(
        IReadOnlyList<AllocationInput> businesses,
        int pool,
        int teacherCapacity)
    {
        ArgumentNullException.ThrowIfNull(businesses);

        var capacity = Math.Max(0, teacherCapacity);
        var effectivePool = Math.Max(0, pool);
        var candidates = businesses.Select(Candidate.From)
            .OrderBy(c => c, PriorityComparer.Instance)
            .ToList();
        var sumOfMax = candidates.Sum(c => c.MaxHours);

        // Havuz hesaplanmamış → "havuz tanımlanmamış" bildirimi, hiçbir öneri üretilmez.
        if (effectivePool == 0)
        {
            return new AllocationResult([], new AllocationDiagnostics(
                Pool: 0,
                TeacherCapacity: capacity,
                SumOfMax: sumOfMax,
                TotalAllocated: 0,
                Undistributed: 0,
                HonoraryCount: 0,
                OutOfBranchHours: 0,
                IsPoolUndefined: true));
        }

        var hours = new int[candidates.Count];
        var pinnedTotal = ApplyPinnedHours(candidates, hours);

        // Sabitlenmiş saatler havuzu aşarsa dağıtılacak bir şey kalmaz (aşım tanılamada görünür).
        var distributablePool = Math.Max(0, effectivePool - pinnedTotal);
        var fundable = SelectFundableBusinesses(candidates, distributablePool);

        foreach (var index in fundable) hours[index] = MinPaidHours;

        DistributeRemainder(
            candidates,
            hours,
            fundable,
            remaining: distributablePool - fundable.Count * MinPaidHours);

        return BuildResult(candidates, hours, effectivePool, capacity, sumOfMax);
    }

    /// <summary>Sabitlenmiş satırları yerleştirir ve toplamlarını döndürür (adım 1).</summary>
    private static int ApplyPinnedHours(IReadOnlyList<Candidate> candidates, int[] hours)
    {
        var total = 0;
        for (var i = 0; i < candidates.Count; i++)
        {
            if (!candidates[i].IsPinned) continue;
            hours[i] = candidates[i].PinnedHours;
            total += hours[i];
        }

        return total;
    }

    /// <summary>
    /// Kalan havuzun ücretli saat verebileceği işletmeleri ağırlık sırasına göre seçer.
    /// Havuz herkese 1 saat vermeye yetmiyorsa en düşük ağırlıklılar dışarıda kalır → fahri.
    /// </summary>
    private static List<int> SelectFundableBusinesses(IReadOnlyList<Candidate> candidates, int remainingPool)
    {
        var eligible = new List<int>();
        for (var i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].IsPinned) continue;
            if (candidates[i].MaxHours < MinPaidHours) continue;
            eligible.Add(i);
        }

        var fundableCount = Math.Min(eligible.Count, remainingPool / MinPaidHours);
        return eligible.Take(fundableCount).ToList();
    }

    /// <summary>
    /// Taban saatler dağıtıldıktan sonra kalan havuzu ağırlık oranında dağıtır (adım 2-4).
    /// Her tur: oransal taban pay → tavana kırp → tamsayı artığını en büyük kalana ver.
    /// Doyan işletmeler bir sonraki turda devre dışı kalır, artan saat kalanlara akar.
    /// </summary>
    private static void DistributeRemainder(
        IReadOnlyList<Candidate> candidates,
        int[] hours,
        IReadOnlyList<int> fundable,
        int remaining)
    {
        while (remaining > 0)
        {
            var active = fundable
                .Where(i => candidates[i].Weight > 0 && RoomLeft(candidates, hours, i) > 0)
                .ToList();
            if (active.Count == 0) return;

            var totalWeight = active.Sum(i => candidates[i].Weight);
            var given = 0;
            var quotaFractions = new List<(decimal Fraction, int Index)>(active.Count);

            foreach (var i in active)
            {
                var quota = remaining * (decimal)candidates[i].Weight / totalWeight;
                var wholePart = Math.Floor(quota);
                var take = Math.Min((int)wholePart, RoomLeft(candidates, hours, i));
                hours[i] += take;
                given += take;
                quotaFractions.Add((quota - wholePart, i));
            }

            given += DistributeLargestRemainder(candidates, hours, quotaFractions, remaining - given);

            // İlerleme yoksa (tüm paylar 0 ve yer kalmadı) sonsuz döngüyü kes.
            if (given == 0) return;
            remaining -= given;
        }
    }

    /// <summary>
    /// Tamsayıya yuvarlamadan artan saatleri en büyük ondalık kalana verir (Hare-Niemeyer).
    /// Beraberlikte öncelik sırası (ağırlık ↓, tavan ↓, alan kodu ↑, kimlik ↑) belirleyicidir.
    /// </summary>
    private static int DistributeLargestRemainder(
        IReadOnlyList<Candidate> candidates,
        int[] hours,
        IReadOnlyList<(decimal Fraction, int Index)> quotaFractions,
        int residue)
    {
        if (residue <= 0) return 0;

        var receivers = quotaFractions
            .Where(q => RoomLeft(candidates, hours, q.Index) > 0)
            .OrderByDescending(q => q.Fraction)
            .ThenBy(q => q.Index)
            .Select(q => q.Index)
            .ToList();

        var given = 0;
        foreach (var index in receivers)
        {
            if (given == residue) break;
            hours[index] += 1;
            given++;
        }

        return given;
    }

    private static int RoomLeft(IReadOnlyList<Candidate> candidates, int[] hours, int index) =>
        candidates[index].MaxHours - hours[index];

    /// <summary>
    /// Satırları üç kovaya ayırır (adım 5). Kovalar aynı ağırlık sırasını izler:
    /// kümülatif saat kapasiteyi (<c>C</c>) aştığı andan itibaren kalan ücretli satırlar
    /// alan dışına önerilir; 0 saatlik satırlar fahridir ve kapasite tüketmez.
    /// </summary>
    private static AllocationBucket[] AssignBuckets(
        IReadOnlyList<Candidate> candidates,
        int[] hours,
        int capacity)
    {
        var buckets = new AllocationBucket[candidates.Count];
        var cumulative = 0;
        var capacityExhausted = false;

        for (var i = 0; i < candidates.Count; i++)
        {
            if (hours[i] <= 0)
            {
                buckets[i] = AllocationBucket.Honorary;
                continue;
            }

            cumulative += hours[i];
            if (cumulative > capacity) capacityExhausted = true;

            buckets[i] = capacityExhausted
                ? AllocationBucket.OutOfBranchSuggested
                : AllocationBucket.InBranchPaid;
        }

        return buckets;
    }

    private static AllocationResult BuildResult(
        IReadOnlyList<Candidate> candidates,
        int[] hours,
        int pool,
        int capacity,
        int sumOfMax)
    {
        var buckets = AssignBuckets(candidates, hours, capacity);
        var lines = new List<AllocationLine>(candidates.Count);

        for (var i = 0; i < candidates.Count; i++)
        {
            lines.Add(new AllocationLine(
                BusinessId: candidates[i].BusinessId,
                BranchCode: candidates[i].BranchCode,
                MaxHours: candidates[i].MaxHours,
                StudentCount: candidates[i].StudentCount,
                Weight: candidates[i].Weight,
                SuggestedHours: hours[i],
                IsPinned: candidates[i].IsPinned,
                IsHonoraryVisit: hours[i] <= 0,
                Bucket: buckets[i]));
        }

        var totalAllocated = lines.Sum(l => l.SuggestedHours);

        return new AllocationResult(lines, new AllocationDiagnostics(
            Pool: pool,
            TeacherCapacity: capacity,
            SumOfMax: sumOfMax,
            TotalAllocated: totalAllocated,
            Undistributed: pool - totalAllocated,
            HonoraryCount: lines.Count(l => l.IsHonoraryVisit),
            OutOfBranchHours: lines
                .Where(l => l.Bucket == AllocationBucket.OutOfBranchSuggested)
                .Sum(l => l.SuggestedHours),
            IsPoolUndefined: false));
    }

    /// <summary>Sınırları normalize edilmiş, ağırlığı önceden hesaplanmış iç girdi.</summary>
    private sealed record Candidate(
        Guid BusinessId,
        string BranchCode,
        int MaxHours,
        int StudentCount,
        long Weight,
        bool IsPinned,
        int PinnedHours)
    {
        internal static Candidate From(AllocationInput input)
        {
            var maxHours = Math.Max(0, input.MaxHours);
            var studentCount = Math.Max(0, input.StudentCount);

            return new Candidate(
                input.BusinessId,
                input.BranchCode ?? string.Empty,
                maxHours,
                studentCount,
                (long)maxHours * studentCount,
                input.IsPinned,
                Math.Max(0, input.PinnedHours));
        }
    }

    /// <summary>
    /// Beraberlik bozma kuralı — deterministik toplam sıralama:
    /// ağırlık ↓ → tavan saat ↓ → alan kodu (ordinal) ↑ → işletme kimliği (ordinal) ↑.
    /// Son basamak benzersiz olduğu için aynı girdi her zaman aynı sırayı üretir.
    /// </summary>
    private sealed class PriorityComparer : IComparer<Candidate>
    {
        internal static readonly PriorityComparer Instance = new();

        public int Compare(Candidate? x, Candidate? y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x is null) return 1;
            if (y is null) return -1;

            var byWeight = y.Weight.CompareTo(x.Weight);
            if (byWeight != 0) return byWeight;

            var byMaxHours = y.MaxHours.CompareTo(x.MaxHours);
            if (byMaxHours != 0) return byMaxHours;

            var byBranch = string.CompareOrdinal(x.BranchCode, y.BranchCode);
            if (byBranch != 0) return byBranch;

            return string.CompareOrdinal(x.BusinessId.ToString(), y.BusinessId.ToString());
        }
    }
}
