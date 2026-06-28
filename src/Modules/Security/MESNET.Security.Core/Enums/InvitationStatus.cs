using Ardalis.SmartEnum;

namespace MESNET.Security.Core.Enums;

public sealed class InvitationStatus : SmartEnum<InvitationStatus>
{
    public static readonly InvitationStatus PendingApproval = new(nameof(PendingApproval), 1, "Onay Bekliyor");
    public static readonly InvitationStatus Approved = new(nameof(Approved), 2, "Onaylandı");
    public static readonly InvitationStatus Rejected = new(nameof(Rejected), 3, "Reddedildi");
    public static readonly InvitationStatus Completed = new(nameof(Completed), 4, "Tamamlandı");
    public static readonly InvitationStatus Expired = new(nameof(Expired), 5, "Süresi Doldu");

    public string Slug { get; }

    private InvitationStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
