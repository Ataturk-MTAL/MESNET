using Ardalis.SmartEnum;

namespace MESNET.Business.Core.Enums;

public sealed class DocumentStatus : SmartEnum<DocumentStatus>
{
    public static readonly DocumentStatus Uploaded = new(nameof(Uploaded), 1, "Yüklendi");
    public static readonly DocumentStatus Approved = new(nameof(Approved), 2, "Onaylandı");
    public static readonly DocumentStatus Rejected = new(nameof(Rejected), 3, "Reddedildi");
    public static readonly DocumentStatus Expired = new(nameof(Expired), 4, "Süresi Doldu");

    public string Slug { get; }

    private DocumentStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
