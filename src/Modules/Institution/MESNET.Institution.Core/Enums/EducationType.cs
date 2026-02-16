using Ardalis.SmartEnum;

namespace MESNET.Institution.Core.Enums;

public sealed class EducationType : SmartEnum<EducationType>
{
    public static readonly EducationType Mesem = new(nameof(Mesem), 1, "MESEM");
    public static readonly EducationType Formal = new(nameof(Formal), 2, "Örgün");

    public string Slug { get; }

    private EducationType(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
