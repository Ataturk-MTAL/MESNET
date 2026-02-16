using Ardalis.SmartEnum;

namespace MESNET.Enrollment.Core.Enums;

public sealed class StudentStatus : SmartEnum<StudentStatus>
{
    public static readonly StudentStatus Registered = new(nameof(Registered), 1, "Kayıtlı");
    public static readonly StudentStatus Applied = new(nameof(Applied), 2, "Başvurdu");
    public static readonly StudentStatus Placed = new(nameof(Placed), 3, "Yerleştirildi");
    public static readonly StudentStatus ActiveInternship = new(nameof(ActiveInternship), 4, "Aktif Staj");
    public static readonly StudentStatus Completed = new(nameof(Completed), 5, "Tamamladı");

    public string Slug { get; }

    private StudentStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public bool IsFinal => this == Completed;
    public bool IsPlaceable => this == Applied || this == Registered;

    private static readonly Dictionary<StudentStatus, HashSet<StudentStatus>> Transitions = new()
    {
        [Registered] = [Applied],
        [Applied] = [Placed],
        [Placed] = [ActiveInternship, Registered],
        [ActiveInternship] = [Completed],
        [Completed] = []
    };

    public bool CanTransitionTo(StudentStatus target)
        => Transitions.TryGetValue(this, out var allowed) && allowed.Contains(target);
}
