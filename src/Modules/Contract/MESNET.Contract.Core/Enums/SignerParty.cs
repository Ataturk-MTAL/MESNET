using Ardalis.SmartEnum;

namespace MESNET.Contract.Core.Enums;

public sealed class SignerParty : SmartEnum<SignerParty>
{
    public static readonly SignerParty Institution = new(nameof(Institution), 1, "Kurum");
    public static readonly SignerParty Business = new(nameof(Business), 2, "İşletme");
    public static readonly SignerParty Student = new(nameof(Student), 3, "Öğrenci");
    public static readonly SignerParty Parent = new(nameof(Parent), 4, "Veli");

    public string Slug { get; }

    private SignerParty(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
