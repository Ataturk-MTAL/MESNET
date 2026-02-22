using Ardalis.SmartEnum;

namespace MESNET.Contract.Core.Enums;

public sealed class ContractDocumentType : SmartEnum<ContractDocumentType>
{
    public static readonly ContractDocumentType SignedContract    = new(nameof(SignedContract),    1, "Islak İmzalı Sözleşme");
    public static readonly ContractDocumentType TerminationLetter = new(nameof(TerminationLetter), 2, "Fesih Belgesi");
    public static readonly ContractDocumentType Other             = new(nameof(Other),             3, "Diğer");

    public string Slug { get; }

    private ContractDocumentType(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
