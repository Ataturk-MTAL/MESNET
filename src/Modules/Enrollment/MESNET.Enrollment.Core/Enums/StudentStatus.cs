using Ardalis.SmartEnum;

namespace MESNET.Enrollment.Core.Enums;

public sealed class StudentStatus : SmartEnum<StudentStatus>
{
    public static readonly StudentStatus Registered = new(nameof(Registered), 1, "Kayıtlı");
    public static readonly StudentStatus Applied = new(nameof(Applied), 2, "Başvurdu");
    public static readonly StudentStatus Placed = new(nameof(Placed), 3, "Yerleştirildi");
    public static readonly StudentStatus ActiveInternship = new(nameof(ActiveInternship), 4, "Aktif Staj");
    public static readonly StudentStatus Completed = new(nameof(Completed), 5, "Tamamladı");
    public static readonly StudentStatus Deregistered = new(nameof(Deregistered), 6, "Kayıt Silindi");

    public string Slug { get; }

    private StudentStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public bool IsFinal => this == Completed || this == Deregistered;
    public bool IsPlaceable => this == Applied || this == Registered;

    private static readonly Dictionary<StudentStatus, HashSet<StudentStatus>> Transitions = new()
    {
        [Registered] = [Applied, Placed, Deregistered],
        [Applied] = [Placed, Deregistered],
        // Fesih sonrası okula atama (#220): öğrenci feshedilince okulda staja yerleştirilir,
        // yani doğrudan yeniden Placed olur. Bu geçişler olmadan durum makinesi otomatik
        // atamayı engeller ve öğrenci hiçbir yere bağlı kalmaz.
        [Placed] = [ActiveInternship, Placed, Registered, Deregistered],
        [ActiveInternship] = [Placed, Completed],
        [Completed] = [],
        [Deregistered] = []
    };

    public bool CanTransitionTo(StudentStatus target)
        => Transitions.TryGetValue(this, out var allowed) && allowed.Contains(target);
}
