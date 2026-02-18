namespace MESNET.Common.Shared.Security;

/// <summary>
/// Rol → Permission eşleştirmesi.
/// Wildcard desteği: "student:*" → student: ile başlayan tüm izinleri kapsar.
/// Kaynak: src/Arch/ActorPermissions.md
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
            "company:*"
        ],
        [MesnetRoles.InstitutionStaff] =
        [
            "user:*",
            Permissions.Institution.View,
            Permissions.Student.View,
            Permissions.Student.Manage,
            Permissions.Internship.View,       // staj listesi görüntüleme
            Permissions.Internship.Manage,     // fesih talebi başlatma, genel yönetim
            Permissions.Internship.Approve,    // fesih onay zinciri (veli ıslak imzası + kendi adımı)
            Permissions.Internship.Contract,   // sözleşme yönetimi
            Permissions.Salary.View,
            Permissions.Salary.Approve,        // dekont onay zinciri
            Permissions.Salary.Parameter,      // asgari ücret güncelleme
            Permissions.Attendance.View,
            Permissions.Attendance.Manage,
            Permissions.Attendance.Approve,
            Permissions.Document.View,
            Permissions.Document.Upload,
            Permissions.Document.Track,
            Permissions.Document.Approve,
            Permissions.Company.View,
            Permissions.Company.Manage,
            Permissions.Company.Document,
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
            Permissions.Communication.ViewMessages,
            Permissions.Communication.SendMessage
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
