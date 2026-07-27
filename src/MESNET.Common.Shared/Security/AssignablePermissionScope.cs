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
        // Müdür yardımcısı (#129): kurum geneli yürütme — "*" değil, kullanıcı yönetimi
        // dışındaki tüm domainler. Kapsam muafiyeti izni bu listeyle DE atanamaz
        // (bkz. NeverDirectlyAssignable).
        [MesnetRoles.DeputyDirector] =
        [
            "institution:", "student:", "internship:", "attendance:", "salary:",
            "document:", "communication:", "coordinator:", "department:", "company:",
        ],
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
        // Usta öğretici (#129): işletme içi dar kapsam — devam ve iletişim.
        [MesnetRoles.MasterTrainer] =
        [
            "company:", "communication:",
        ],
        [MesnetRoles.Student] =
        [
            "student:", "communication:",
        ],
    };

    /// <summary>
    /// <b>Hiçbir yapılandırmayla bireysel (direct) atanamayacak izinler (#126).</b>
    ///
    /// <para>Bunlar <b>kapsam muafiyeti</b> izinleridir: erişim değil, "hangi verinin"
    /// sorusunun cevabını genişletirler. Yalnız <see cref="RolePermissionMap"/> üzerinden,
    /// role bağlı olarak gelebilirler.</para>
    ///
    /// <para><b>Neden sabit kodlanmış:</b> <see cref="Defaults"/> yapılandırılabilir ve
    /// <c>PUT /api/security/permission-scopes</c> ile çalışma zamanında değiştirilebilir; bu
    /// uç yalnız <c>user:roles:manage</c> ister ve o izin müdür yardımcısında da vardır.
    /// Sabit liste olmasaydı, müdür yardımcısı önce <c>DepartmentHead</c>'in atanabilir
    /// domain listesine <c>institution:</c> ekler, sonra bir alan şefine
    /// <c>institution:distribution:all-branches</c> vererek kapsam kontrolünü tümden
    /// kaldırabilirdi. Kural mutlaktır, yapılandırma onu gevşetemez.</para>
    ///
    /// <para>Benzer izinler ileride eklenirse <b>tek yer</b> burasıdır.</para>
    /// </summary>
    public static readonly IReadOnlySet<string> NeverDirectlyAssignable =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Permissions.Institution.AllBranches,
        };

    /// <summary>
    /// Verilen rollere sahip bir kullanıcıya bu yetki DIRECT olarak atanabilir mi?
    /// <paramref name="scopeByRole"/> yapılandırılmış kapsam haritasıdır (yoksa <see cref="Defaults"/> geçilir).
    /// </summary>
    public static bool CanAssign(
        IReadOnlyDictionary<string, string[]> scopeByRole, IEnumerable<string> userRoles, string permission)
    {
        // Kapsam muafiyeti izinleri hiçbir koşulda bireysel atanamaz — "*" kapsamı ve
        // yapılandırılmış domain listeleri bu kuralı GEVŞETEMEZ (#126).
        if (NeverDirectlyAssignable.Contains(permission))
            return false;

        var allowed = userRoles
            .SelectMany(r => scopeByRole.TryGetValue(r, out var v) ? v : [])
            .ToHashSet();
        if (allowed.Contains(All)) return true;
        return allowed.Any(prefix => permission.StartsWith(prefix, StringComparison.Ordinal));
    }
}
