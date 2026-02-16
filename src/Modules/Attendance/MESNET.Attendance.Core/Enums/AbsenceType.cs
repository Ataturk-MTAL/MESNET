using Ardalis.SmartEnum;

namespace MESNET.Attendance.Core.Enums;

public sealed class AbsenceType : SmartEnum<AbsenceType>
{
    public static readonly AbsenceType Excused = new(nameof(Excused), 1, "Mazeretli");
    public static readonly AbsenceType Unexcused = new(nameof(Unexcused), 2, "Mazeretsiz");
    public static readonly AbsenceType HealthReport = new(nameof(HealthReport), 3, "Sağlık Raporu");

    public string Slug { get; }

    private AbsenceType(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }

    public bool AffectsSalary => this == Unexcused;
}
