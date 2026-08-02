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
        // Ulusal (kurum üstü) domain (#147). Listede olması yalnız arayüzün domaini
        // gösterebilmesi içindir; altındaki izinler NeverDirectlyAssignable olduğu için
        // bireysel atanamaz.
        "platform:",
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
        // "attendance:" #172 ile eklendi (devamsızlık girişi + sağlık raporu yükleme); hüküm
        // doğuran iki izin NeverDirectlyAssignable listesindedir, bu prefix onları açmaz.
        [MesnetRoles.MasterTrainer] =
        [
            "company:", "attendance:", "communication:",
        ],
        // İşletme insan kaynakları (#172): usta öğreticiyle aynı dar kapsam.
        [MesnetRoles.CompanyHR] =
        [
            "company:", "attendance:", "communication:",
        ],
        [MesnetRoles.Student] =
        [
            "student:", "communication:",
        ],
        // Veli (#174): öğrenciyle aynı dar kapsam. "attendance:" ve "internship:" bilinçli
        // olarak YOK — veliye o domainlerden bireysel izin atanabilseydi, kapsamı bağ kaydıyla
        // sınırlı olan bir kullanıcı okul tarafının uçlarına erişebilirdi.
        [MesnetRoles.Parent] =
        [
            "student:", "communication:",
        ],
        // Sistem yöneticisi (#147): yalnız ulusal domain. Kurum domainlerinden hiçbiri yok —
        // bu rol kurum verisine yetki dağıtamaz.
        [MesnetRoles.SystemAdmin] =
        [
            "platform:",
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
    /// <para><b>Ulusal (kurum üstü) izinler de buradadır (#147).</b> Aynı gerekçe, bir basamak
    /// yukarısı: <c>InstitutionManager</c>'ın atanabilir kapsamı <see cref="All"/> (<c>"*"</c>)
    /// olduğu için, bu liste olmasaydı bir okul müdürü <c>platform:parameter:manage</c>'i
    /// istediği kullanıcıya bireysel atayıp ulusal/kurum ayrımını tümden kaldırabilirdi.</para>
    ///
    /// <para><b>Hüküm izinleri de buradadır (#172).</b> <c>attendance:direct-entry</c> ve
    /// <c>attendance:health-report:direct</c> erişim değil <b>onay muafiyeti</b> açar: sahibinin
    /// girdiği kayıt koordinatör öğretmen onayına düşmez, doğrudan geçerli olur ve ücret
    /// kesintisini kaldırır. <c>CompanyManager</c>, <c>MasterTrainer</c> ve <c>CompanyHR</c>'ın
    /// atanabilir domain listesinde <c>attendance:</c> vardır (devamsızlık girişi ve rapor
    /// yükleme için gerekli); bu liste olmasaydı müdür yardımcısı bir işletme yetkilisine
    /// <c>attendance:health-report:direct</c>'i bireysel atayıp onay zincirini tümden
    /// kaldırabilirdi — yani ödemeyi yapan taraf kendi kesintisini iptal edebilirdi.</para>
    ///
    /// <para><b>İki taraflı onayın okul adımı da buradadır (#177).</b>
    /// <c>attendance:leave:approve</c> ücretli izni resmîleştirir ve devamsızlık kayıtlarını
    /// doğurur. İşletme rollerinin atanabilir domain listesinde <c>attendance:</c> vardır; bu
    /// liste olmasaydı okul adımı bir işletme kullanıcısına bireysel atanabilir ve zincir tek
    /// tarafa çökerdi. "İşletme onayını veren okul onayını veremez" kuralı bunu tek başına
    /// kapatmaz — ikinci bir işletme kullanıcısı okul adımını yapardı. İşletme adımının kendisi
    /// (<c>attendance:leave:business-approve</c>) listede DEĞİLDİR: onu izin değil
    /// <c>business_id</c> kapsamı sınırlar, okul kullanıcısına atansa bile işe yaramaz.</para>
    ///
    /// <para>Benzer izinler ileride eklenirse <b>tek yer</b> burasıdır.</para>
    /// </summary>
    public static readonly IReadOnlySet<string> NeverDirectlyAssignable =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Permissions.Institution.AllBranches,
            Permissions.Platform.ParameterManage,
            Permissions.Attendance.DirectEntry,
            Permissions.Attendance.HealthReportDirect,
            Permissions.Attendance.LeaveApprove,
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
