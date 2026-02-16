using Ardalis.SmartEnum;

namespace MESNET.Coordination.Core.Enums;

/// <summary>
/// Akademik dönem
/// </summary>
public sealed class AcademicSemester : SmartEnum<AcademicSemester>
{
    public static readonly AcademicSemester Fall = new(nameof(Fall), 1, "Güz");
    public static readonly AcademicSemester Spring = new(nameof(Spring), 2, "Bahar");

    private AcademicSemester(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public string Slug { get; }
}
