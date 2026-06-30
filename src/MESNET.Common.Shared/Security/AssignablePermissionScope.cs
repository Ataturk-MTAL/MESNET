namespace MESNET.Common.Shared.Security;

/// <summary>
/// Bir role sahip kullanıcıya DIRECT (bireysel) olarak atanabilecek yetkilerin sınırı.
/// "yetkili olan herkes yapabilir" ilkesinin guardrail'i: yetki kime atanabilir KISITLI olmalı.
/// Kapsam haritası YAPILANDIRILABILIR (PermissionScopeConfig document'i ile yönetilir);
/// buradaki <see cref="Defaults"/> yalnızca ilk seed / fallback'tir.
/// </summary>
public static class AssignablePermissionScope
{
    public const string All = "*";

    /// <summary>Sistemdeki yetki domain'leri (prefix). UI'da rol başına seçilebilir kümeyi oluşturur.</summary>
    public static readonly string[] AllDomains =
    [
        "institution:", "company:", "student:", "internship:", "attendance:",
        "salary:", "document:", "communication:", "coordinator:", "department:", "user:",
    ];

    /// <summary>Varsayılan rol → atanabilir yetki domain (prefix) kümesi. Config yoksa kullanılır.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> Defaults = new Dictionary<string, string[]>
    {
        [MesnetRoles.InstitutionManager] = [All],
        [MesnetRoles.InstitutionStaff] =
        [
            "institution:", "student:", "internship:", "attendance:", "salary:",
            "document:", "communication:", "coordinator:", "department:", "company:",
        ],
        [MesnetRoles.Teacher] =
        [
            "coordinator:", "internship:", "attendance:", "document:", "communication:", "student:",
        ],
        [MesnetRoles.DepartmentHead] =
        [
            "coordinator:", "internship:", "attendance:", "document:", "communication:", "student:", "department:",
        ],
        [MesnetRoles.CompanyManager] =
        [
            "company:", "attendance:", "communication:",
        ],
        [MesnetRoles.Student] =
        [
            "student:", "communication:",
        ],
    };

    /// <summary>
    /// Verilen rollere sahip bir kullanıcıya bu yetki DIRECT olarak atanabilir mi?
    /// <paramref name="scopeByRole"/> yapılandırılmış kapsam haritasıdır (yoksa <see cref="Defaults"/> geçilir).
    /// </summary>
    public static bool CanAssign(
        IReadOnlyDictionary<string, string[]> scopeByRole, IEnumerable<string> userRoles, string permission)
    {
        var allowed = userRoles
            .SelectMany(r => scopeByRole.TryGetValue(r, out var v) ? v : [])
            .ToHashSet();
        if (allowed.Contains(All)) return true;
        return allowed.Any(prefix => permission.StartsWith(prefix, StringComparison.Ordinal));
    }
}
