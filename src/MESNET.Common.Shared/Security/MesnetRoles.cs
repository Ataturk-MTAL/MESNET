namespace MESNET.Common.Shared.Security;

/// <summary>
/// MESNET Phase 1 rol sabitleri.
/// Keycloak realm role adlarıyla birebir eşleşir.
/// </summary>
public static class MesnetRoles
{
    public const string InstitutionManager = "InstitutionManager";
    public const string InstitutionStaff = "InstitutionStaff";
    public const string Teacher = "Teacher";
    public const string Student = "Student";
    public const string DepartmentHead = "DepartmentHead";
    public const string CompanyManager = "CompanyManager";

    public static IReadOnlyList<string> All { get; } =
    [
        InstitutionManager,
        InstitutionStaff,
        Teacher,
        Student,
        DepartmentHead,
        CompanyManager
    ];
}
