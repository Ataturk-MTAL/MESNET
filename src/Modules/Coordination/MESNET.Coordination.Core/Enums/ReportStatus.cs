using Ardalis.SmartEnum;

namespace MESNET.Coordination.Core.Enums;

public sealed class ReportStatus : SmartEnum<ReportStatus>
{
    public static readonly ReportStatus Draft = new(nameof(Draft), 1, "Taslak");
    public static readonly ReportStatus Submitted = new(nameof(Submitted), 2, "Gönderildi");
    public static readonly ReportStatus Approved = new(nameof(Approved), 3, "Onaylandı");

    private ReportStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public string Slug { get; }
}
