using Ardalis.SmartEnum;

namespace MESNET.Business.Core.Enums;

public sealed class DocumentType : SmartEnum<DocumentType>
{
    public static readonly DocumentType MasteryCertificate = new(nameof(MasteryCertificate), 1, "Ustalık Belgesi");
    public static readonly DocumentType MasterInstructorCertificate = new(nameof(MasterInstructorCertificate), 2, "Usta Öğreticilik Belgesi");

    public string Slug { get; }

    private DocumentType(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
