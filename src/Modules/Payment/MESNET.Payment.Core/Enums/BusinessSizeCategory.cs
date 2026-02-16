using Ardalis.SmartEnum;

namespace MESNET.Payment.Core.Enums;

public sealed class BusinessSizeCategory : SmartEnum<BusinessSizeCategory>
{
    public static readonly BusinessSizeCategory Large = new(nameof(Large), 1, "Büyük İşletme");
    public static readonly BusinessSizeCategory Small = new(nameof(Small), 2, "Küçük İşletme");

    public string Slug { get; }

    private BusinessSizeCategory(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
