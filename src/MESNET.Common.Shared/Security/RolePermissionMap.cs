namespace MESNET.Common.Shared.Security;

/// <summary>
/// Rol → Permission eşleştirmesi.
/// Wildcard desteği: "student:*" → student: ile başlayan tüm izinleri kapsar.
/// Kaynak: src/Docs/docs/actors/permissions.md
/// </summary>
public static class RolePermissionMap
{
    private static readonly Dictionary<string, IReadOnlyList<string>> Mappings = new()
    {
        [MesnetRoles.InstitutionManager] =
        [
            "institution:*",
            "student:*",
            "internship:*",
            "attendance:*",
            "salary:*",
            "document:*",
            "communication:*",
            "user:*",
            "coordinator:*",
            "department:*",
            "company:*",
            // Kurum geneli alan muafiyeti (#126) — "institution:*" zaten kapsar,
            // güvenlik kararı olduğu için açıkça yazılır.
            Permissions.Institution.AllBranches,
            // Kurum geneli koordinasyon yapılandırması (#130) — "institution:*" zaten kapsar,
            // güvenlik kararı olduğu için açıkça yazılır.
            Permissions.Institution.CoordinationConfigManage
        ],
        // Müdür yardımcısı (#129). Kaynak: actors.md → "Müdür Yardımcısı" —
        // staj işlemleri koordinasyonu, evrak takibi ve onayı, öğretmen görevlendirmeleri,
        // dekont ve maaş süreçleri yönetimi.
        // Bu demet #129 öncesinde InstitutionStaff'ta duruyordu; gerçekte müdür yardımcısının
        // demetiydi (yorumları da öyle diyordu). Ayrı role taşındı, InstitutionStaff daraltıldı.
        [MesnetRoles.DeputyDirector] =
        [
            "user:*",                              // davet onayı, kullanıcı/rol yönetimi
            "department:*",                        // öğretmen görevlendirme + işletme dağıtımı
            // Müdür yardımcısı tüm alanların dağıtımını yönetebilir (#126).
            // DepartmentHead bu izni ALMAZ — alan kapsamı kontrolü ona uygulanır.
            // InstitutionStaff da ALMAZ (#129) — koordinasyon dağıtımı onun görevi değil.
            Permissions.Institution.AllBranches,
            // Kurum geneli koordinasyon yapılandırması (#130): mesafe-saat kuralları,
            // büyükşehir sınırı ve azami haftalık ek ders saati. Alan bazlı değil kurum
            // düzeyi bir ayardır; DepartmentHead ve InstitutionStaff ALMAZ.
            Permissions.Institution.CoordinationConfigManage,
            Permissions.Institution.View,
            Permissions.Student.View,
            Permissions.Student.Manage,
            Permissions.Internship.View,       // staj listesi görüntüleme
            Permissions.Internship.Manage,     // fesih talebi başlatma, genel yönetim
            Permissions.Internship.Approve,    // fesih onay zinciri (veli ıslak imzası + kendi adımı)
            Permissions.Internship.Contract,   // sözleşme yönetimi
            Permissions.Salary.View,
            Permissions.Salary.Calculate,
            Permissions.Salary.Approve,        // dekont onay zinciri
            Permissions.Salary.Parameter,      // asgari ücret güncelleme
            Permissions.Attendance.View,
            Permissions.Attendance.Manage,
            Permissions.Attendance.Report,
            Permissions.Attendance.Approve,
            Permissions.Document.View,
            Permissions.Document.Upload,
            Permissions.Document.Verify,
            Permissions.Document.Track,
            Permissions.Document.Approve,      // evrak onayı
            Permissions.Company.View,
            Permissions.Company.Manage,
            Permissions.Company.Document,
            Permissions.Communication.ViewMessages,
            Permissions.Communication.SendMessage
        ],
        // Kurum yetkilendirdiği personel (#129). Kaynak: actors.md → "Kurum Yetkilendirdiği
        // Personel" — öğrenci kayıt işlemleri, belge doğrulama, devamsızlık takibi,
        // maaş hesaplamaları. Yürütür ama ONAYLAMAZ ve kullanıcı/koordinasyon yönetmez:
        // "user:*", "department:*", *.Approve ve kapsam muafiyeti DeputyDirector'dedir.
        [MesnetRoles.InstitutionStaff] =
        [
            Permissions.Institution.View,
            Permissions.Student.View,
            Permissions.Student.Manage,        // öğrenci kayıt işlemleri
            Permissions.Internship.View,       // öğrenci/devamsızlık işi staj bağlamında yürür
            Permissions.Salary.View,
            Permissions.Salary.Calculate,      // maaş hesaplamaları (onay müdür yardımcısında)
            Permissions.Attendance.View,
            Permissions.Attendance.Manage,     // devamsızlık takibi
            Permissions.Attendance.Report,
            Permissions.Document.View,
            Permissions.Document.Upload,
            Permissions.Document.Verify,       // belge doğrulama
            Permissions.Document.Track,
            Permissions.Company.View,
            Permissions.Communication.ViewMessages,
            Permissions.Communication.SendMessage
        ],
        [MesnetRoles.Teacher] =
        [
            Permissions.Student.View,
            Permissions.Student.Manage,
            Permissions.Internship.View,
            Permissions.Internship.Review,     // başvuru inceleme
            Permissions.Internship.Approve,    // fesih onay zincirinde kendi adımı
            Permissions.Attendance.View,
            Permissions.Attendance.Manage,
            Permissions.Attendance.Report,
            Permissions.Attendance.Approve,
            Permissions.Salary.View,
            Permissions.Salary.Approve,        // dekont ilk onay (öğretmen adımı)
            Permissions.Coordinator.Schedule,
            Permissions.Coordinator.Visit,
            Permissions.Coordinator.Report,
            Permissions.Coordinator.Communication,
            Permissions.Document.View,
            Permissions.Document.Upload,
            Permissions.Communication.ViewMessages,
            Permissions.Communication.SendMessage
        ],
        [MesnetRoles.Student] =
        [
            Permissions.Student.ViewOwn,
            Permissions.Student.UpdateOwn,
            Permissions.Internship.ViewOwn,
            Permissions.Internship.Apply,
            Permissions.Attendance.ViewOwn,
            Permissions.Salary.ViewOwn,
            Permissions.Communication.ViewMessages,
            Permissions.Communication.SendMessage,
            Permissions.Communication.ReportIssue
        ],
        [MesnetRoles.DepartmentHead] =
        [
            "department:*",
            Permissions.Student.View,
            Permissions.Coordinator.Schedule,
            Permissions.Attendance.View,
            Permissions.Attendance.Report,
            Permissions.Attendance.Delete,
            Permissions.Document.View,
            Permissions.Communication.ViewMessages,
            Permissions.Communication.SendMessage
        ],
        [MesnetRoles.CompanyManager] =
        [
            Permissions.Company.View,
            Permissions.Company.Manage,
            Permissions.Company.Document,
            Permissions.Company.Student,
            Permissions.Company.Attendance,
            Permissions.Company.UploadReceipt,
            Permissions.Company.MasterTrainer,
            Permissions.Company.RequestStudent,
            Permissions.Company.EnterGrade,
            Permissions.Attendance.Manage,
            Permissions.Communication.ViewMessages,
            Permissions.Communication.SendMessage
        ],
        // Usta öğretici (#129) — işletme içinde DAR yetkili. CompanyManager'ın geniş demetini
        // ALMAZ: öğrenci talebi, dekont yükleme ve işletme belge yönetimi onda kalır.
        [MesnetRoles.MasterTrainer] =
        [
            Permissions.Company.Attendance,    // devam takibi
            Permissions.Company.EnterGrade,    // dönem notu girişi
            Permissions.Student.View,          // kendi öğrencileri
            "communication:*"
        ]
    };

    private static readonly Lazy<IReadOnlyList<string>> AllPermissions = new(() => Permissions.GetAll());

    /// <summary>
    /// Verilen roller için tüm permission'ları döndürür.
    /// Wildcard ifadeler ("student:*") genişletilir.
    /// </summary>
    public static IReadOnlyList<string> GetPermissionsForRoles(IEnumerable<string> roles)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var role in roles)
        {
            if (!Mappings.TryGetValue(role, out var rolePermissions))
                continue;

            foreach (var permission in rolePermissions)
            {
                if (permission.EndsWith(":*"))
                {
                    var prefix = permission[..^1]; // "student:*" → "student:"
                    foreach (var expanded in AllPermissions.Value)
                    {
                        if (expanded.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            permissions.Add(expanded);
                    }
                }
                else
                {
                    permissions.Add(permission);
                }
            }
        }

        return permissions.ToList();
    }

    /// <summary>
    /// Belirli bir rolün ham permission listesini döndürür (wildcard genişletilmez).
    /// UI'da rol detayı göstermek için kullanılır.
    /// </summary>
    public static IReadOnlyList<string> GetRawPermissionsForRole(string role)
    {
        return Mappings.TryGetValue(role, out var permissions)
            ? permissions
            : [];
    }

    /// <summary>
    /// Belirli bir permission'ın wildcard eşleşmesiyle kontrol eder.
    /// Kullanıcıda "student:*" varsa, "student:view" için true döner.
    /// </summary>
    public static bool MatchesPermission(string userPermission, string requiredPermission)
    {
        if (string.Equals(userPermission, requiredPermission, StringComparison.OrdinalIgnoreCase))
            return true;

        if (userPermission.EndsWith(":*"))
        {
            var prefix = userPermission[..^1];
            return requiredPermission.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }
}
