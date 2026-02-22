using Ardalis.SmartEnum;

namespace MESNET.Institution.Core.Enums;

public sealed class AcademicPeriodStatus : SmartEnum<AcademicPeriodStatus>
{
    public static readonly AcademicPeriodStatus Active = new(nameof(Active), 1, "Aktif");
    public static readonly AcademicPeriodStatus Closed = new(nameof(Closed), 2, "Kapatılmış");

    public string Slug { get; }

    private AcademicPeriodStatus(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
