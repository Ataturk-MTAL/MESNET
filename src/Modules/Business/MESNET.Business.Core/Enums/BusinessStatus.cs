using Ardalis.SmartEnum;

namespace MESNET.Business.Core.Enums;

public sealed class BusinessStatus : SmartEnum<BusinessStatus>
{
    public static readonly BusinessStatus PendingApproval = new(nameof(PendingApproval), 1, "Onay Bekliyor");
    public static readonly BusinessStatus Active = new(nameof(Active), 2, "Aktif");
    public static readonly BusinessStatus Rejected = new(nameof(Rejected), 3, "Reddedildi");
    public static readonly BusinessStatus Inactive = new(nameof(Inactive), 4, "Pasif");
    public static readonly BusinessStatus Closed = new(nameof(Closed), 5, "Kapatılmış");

    public string Slug { get; }

    private BusinessStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public bool IsFinal => this == Closed;
    public bool IsOperational => this == Active;

    private static readonly Dictionary<BusinessStatus, HashSet<BusinessStatus>> Transitions = new()
    {
        [PendingApproval] = [Active, Rejected],
        [Active] = [Inactive, Closed],
        [Rejected] = [],
        [Inactive] = [Active, Closed],
        [Closed] = []
    };

    public bool CanTransitionTo(BusinessStatus target)
        => Transitions.TryGetValue(this, out var allowed) && allowed.Contains(target);
}
