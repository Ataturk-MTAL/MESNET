using Ardalis.SmartEnum;

namespace MESNET.Coordination.Core.Enums;

/// <summary>
/// Akademik yarıyıl (MEB terminolojisi)
/// Name: serialization key (Fall/Spring/Summer)
/// Slug: Türkçe UI gösterimi (1. Dönem / 2. Dönem / Yaz Dönemi)
/// Number: sayısal karşılaştırma (1, 2, 3)
/// </summary>
public sealed class AcademicSemester : SmartEnum<AcademicSemester>
{
    public static readonly AcademicSemester Fall   = new(nameof(Fall),   1, "1. Dönem", 1);
    public static readonly AcademicSemester Spring = new(nameof(Spring), 2, "2. Dönem", 2);
    public static readonly AcademicSemester Summer = new(nameof(Summer), 3, "Yaz Dönemi", 3);

    private AcademicSemester(string name, int value, string slug, int number) : base(name, value)
    {
        Slug = slug;
        Number = number;
    }

    public string Slug { get; }
    public int Number { get; }
}
