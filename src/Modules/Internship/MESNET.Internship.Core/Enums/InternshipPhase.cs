using Ardalis.SmartEnum;

namespace MESNET.Internship.Core.Enums;

public sealed class InternshipPhase : SmartEnum<InternshipPhase>
{
    public static readonly InternshipPhase Placed = new(nameof(Placed), 1, "Yerleşti");
    public static readonly InternshipPhase AwaitingContract = new(nameof(AwaitingContract), 2, "Sözleşme Bekleniyor");
    public static readonly InternshipPhase Active = new(nameof(Active), 3, "Aktif");
    public static readonly InternshipPhase TerminationInProgress = new(nameof(TerminationInProgress), 4, "Fesih Sürecinde");
    public static readonly InternshipPhase Terminated = new(nameof(Terminated), 5, "Feshedildi");
    public static readonly InternshipPhase Completed = new(nameof(Completed), 6, "Tamamlandı");

    public string Slug { get; }

    private InternshipPhase(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public bool IsFinal => this == Terminated || this == Completed;
    public bool IsActive => this == Active;

    private static readonly Dictionary<InternshipPhase, HashSet<InternshipPhase>> Transitions = new()
    {
        [Placed] = [AwaitingContract],
        [AwaitingContract] = [Active],
        [Active] = [TerminationInProgress, Completed],
        [TerminationInProgress] = [Terminated],
        [Terminated] = [],
        [Completed] = []
    };

    public bool CanTransitionTo(InternshipPhase target)
        => Transitions.TryGetValue(this, out var allowed) && allowed.Contains(target);
}
