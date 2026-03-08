using Ardalis.SmartEnum;

namespace MESNET.Common.Shared.Enums;

/// <summary>
/// Eğitim tipi: Örgün (Formal) veya MESEM (Mesleki Eğitim Merkezi).
/// Aynı alan (branch) hem örgün hem MESEM'de olabilir.
/// </summary>
public sealed class EducationType : SmartEnum<EducationType>
{
    public static readonly EducationType Formal = new(nameof(Formal), 1, "Örgün");
    public static readonly EducationType Mesem = new(nameof(Mesem), 2, "MESEM");

    public string Slug { get; }

    private EducationType(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
