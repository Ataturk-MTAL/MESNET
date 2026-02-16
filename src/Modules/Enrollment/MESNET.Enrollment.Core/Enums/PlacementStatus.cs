using Ardalis.SmartEnum;

namespace MESNET.Enrollment.Core.Enums;

public sealed class PlacementStatus : SmartEnum<PlacementStatus>
{
    public static readonly PlacementStatus Matched = new(nameof(Matched), 1, "Eşleştirildi");
    public static readonly PlacementStatus Active = new(nameof(Active), 2, "Aktif");
    public static readonly PlacementStatus Transferred = new(nameof(Transferred), 3, "Transfer Edildi");
    public static readonly PlacementStatus Completed = new(nameof(Completed), 4, "Tamamlandı");
    public static readonly PlacementStatus Cancelled = new(nameof(Cancelled), 5, "İptal Edildi");

    public string Slug { get; }

    private PlacementStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public bool IsFinal => this == Completed || this == Transferred || this == Cancelled;
    public bool IsActive => this == Active;

    private static readonly Dictionary<PlacementStatus, HashSet<PlacementStatus>> Transitions = new()
    {
        [Matched] = [Active, Cancelled],
        [Active] = [Transferred, Completed],
        [Transferred] = [],
        [Completed] = [],
        [Cancelled] = []
    };

    public bool CanTransitionTo(PlacementStatus target)
        => Transitions.TryGetValue(this, out var allowed) && allowed.Contains(target);
}
