using Ardalis.SmartEnum;

namespace MESNET.Coordination.Core.Enums;

/// <summary>
/// Saat dağıtım önerisinde bir işletme satırının düştüğü kova.
/// Üç kova da aynı ağırlık (w = tavan saat × öğrenci sayısı) sıralamasını izler.
/// </summary>
public sealed class AllocationBucket : SmartEnum<AllocationBucket>
{
    /// <summary>Ağırlık sırasının üstü — alan öğretmeni kapasitesi (C) yettiği kadar.</summary>
    public static readonly AllocationBucket InBranchPaid =
        new(nameof(InBranchPaid), 1, "Alan içi ücretli");

    /// <summary>Havuzda saat var ama alanın öğretmeninde boş saat yok — alan dışı öğretmene önerilir.</summary>
    public static readonly AllocationBucket OutOfBranchSuggested =
        new(nameof(OutOfBranchSuggested), 2, "Alan dışı öneri");

    /// <summary>Havuz yetmediği için 0 saat kalan işletme — fahri ziyaret.</summary>
    public static readonly AllocationBucket Honorary =
        new(nameof(Honorary), 3, "Fahri");

    private AllocationBucket(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public string Slug { get; }
}
