using Ardalis.SmartEnum;

namespace MESNET.Institution.Core.Enums;

public sealed class StaffRole : SmartEnum<StaffRole>
{
    public static readonly StaffRole Principal = new(nameof(Principal), 1, "Müdür");
    public static readonly StaffRole VicePrincipal = new(nameof(VicePrincipal), 2, "Müdür Yardımcısı");
    public static readonly StaffRole Coordinator = new(nameof(Coordinator), 3, "Koordinatör");
    public static readonly StaffRole DepartmentHead = new(nameof(DepartmentHead), 4, "Alan Şefi");
    public static readonly StaffRole WorkshopHead = new(nameof(WorkshopHead), 5, "Atölye Şefi");
    public static readonly StaffRole Staff = new(nameof(Staff), 6, "Personel");

    public string Slug { get; }

    private StaffRole(string name, int value, string slug) : base(name, value)
    {
        Slug = slug;
    }
}
