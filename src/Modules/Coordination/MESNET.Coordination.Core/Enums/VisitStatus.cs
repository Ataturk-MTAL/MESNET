using Ardalis.SmartEnum;

namespace MESNET.Coordination.Core.Enums;

public sealed class VisitStatus : SmartEnum<VisitStatus>
{
    public static readonly VisitStatus Draft = new(nameof(Draft), 1, "Taslak");
    public static readonly VisitStatus Submitted = new(nameof(Submitted), 2, "Gönderildi");
    public static readonly VisitStatus Approved = new(nameof(Approved), 3, "Onaylandı");

    private VisitStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public string Slug { get; }
}
