using Ardalis.SmartEnum;

namespace MESNET.Enrollment.Core.Enums;

public sealed class ApplicationSource : SmartEnum<ApplicationSource>
{
    public static readonly ApplicationSource StudentApplication = new(nameof(StudentApplication), 1, "Öğrenci Başvurusu");
    public static readonly ApplicationSource BusinessRequest = new(nameof(BusinessRequest), 2, "İşletme Talebi");
    public static readonly ApplicationSource InstitutionAssignment = new(nameof(InstitutionAssignment), 3, "Kurum Ataması");

    public string Slug { get; }

    private ApplicationSource(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
