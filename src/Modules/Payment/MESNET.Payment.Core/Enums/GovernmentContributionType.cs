using Ardalis.SmartEnum;

namespace MESNET.Payment.Core.Enums;

public sealed class GovernmentContributionType : SmartEnum<GovernmentContributionType>
{
    public static readonly GovernmentContributionType MemStudent = new(nameof(MemStudent), 1, "MEB Öğrencisi");
    public static readonly GovernmentContributionType NonMemLarge = new(nameof(NonMemLarge), 2, "MEM Dışı - Büyük");
    public static readonly GovernmentContributionType NonMemSmall = new(nameof(NonMemSmall), 3, "MEM Dışı - Küçük");
    public static readonly GovernmentContributionType PublicInstitution = new(nameof(PublicInstitution), 4, "Kamu Kurumu");

    public string Slug { get; }

    private GovernmentContributionType(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
