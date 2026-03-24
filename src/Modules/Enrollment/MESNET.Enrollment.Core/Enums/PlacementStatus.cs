using Ardalis.SmartEnum;

namespace MESNET.Enrollment.Core.Enums;

public sealed class PlacementStatus : SmartEnum<PlacementStatus>
{
    public static readonly PlacementStatus Matched = new(nameof(Matched), 1, "Yerleştirildi");
    public static readonly PlacementStatus Active = new(nameof(Active), 2, "Aktif");
    public static readonly PlacementStatus Completed = new(nameof(Completed), 4, "Tamamlandı");
    public static readonly PlacementStatus Cancelled = new(nameof(Cancelled), 5, "Fesih Yapıldı");
    public static readonly PlacementStatus FailedToComplete = new(nameof(FailedToComplete), 6, "Tamamlayamadı");

    public string Slug { get; }

    private PlacementStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public bool IsFinal => this == Completed || this == Cancelled || this == FailedToComplete;
    public bool IsActive => this == Active;

    private static readonly Dictionary<PlacementStatus, HashSet<PlacementStatus>> Transitions = new()
    {
        [Matched] = [Active, Cancelled],
        [Active] = [Completed, Cancelled, FailedToComplete],
        [Completed] = [],
        [Cancelled] = [],
        [FailedToComplete] = []
    };

    public bool CanTransitionTo(PlacementStatus target)
        => Transitions.TryGetValue(this, out var allowed) && allowed.Contains(target);
}
