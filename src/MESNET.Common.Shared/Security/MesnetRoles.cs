namespace MESNET.Common.Shared.Security;

/// <summary>
/// Bir rolün kimliği ve Türkçe arayüz karşılığı (#129).
///
/// <para>SmartEnum <c>Name</c>/<c>Slug</c> deseninin aynısı: <see cref="Name"/> İngilizcedir ve
/// serialize edilir (Keycloak realm rol adı, token claim'i, API gövdesi); <see cref="Label"/> ve
/// <see cref="Description"/> yalnız gösterim içindir. Etiketler burada tutulur ki arayüz kendi
/// rol listesini elle yazmasın — sapan liste #129'un kök nedeniydi.</para>
/// </summary>
public sealed record MesnetRoleInfo(string Name, string Label, string Description);

/// <summary>
/// MESNET Phase 1 rol sabitleri.
/// Keycloak realm role adlarıyla birebir eşleşir
/// (<c>src/MESNET.AppHost/keycloak/mesnet-realm.json</c> → <c>roles.realm</c>).
///
/// <para><b>Tek doğruluk kaynağı:</b> rol adı hiçbir yerde elle yazılmaz — arayüz listesini
/// <c>GET /api/security/roles</c> ucundan alır, sunucu tarafı doğrulaması <see cref="All"/>
/// üzerinden yapılır. Listeyi genişletirken <see cref="RolePermissionMap"/>,
/// <see cref="AssignablePermissionScope.Defaults"/> ve realm JSON birlikte güncellenmelidir;
/// sapmayı kilitleyen testler: <c>tests/MESNET.Security.UnitTests/RoleModelDriftTests.cs</c>.</para>
/// </summary>
public static class MesnetRoles
{
    public const string InstitutionManager = "InstitutionManager";

    /// <summary>Müdür yardımcısı (#129). Önceden <see cref="InstitutionStaff"/> ile karıştırılıyordu.</summary>
    public const string DeputyDirector = "DeputyDirector";

    public const string InstitutionStaff = "InstitutionStaff";
    public const string Teacher = "Teacher";
    public const string Student = "Student";
    public const string DepartmentHead = "DepartmentHead";
    public const string CompanyManager = "CompanyManager";

    /// <summary>Usta öğretici (#129) — işletmede devam ve not girişi yapar, dar yetkilidir.</summary>
    public const string MasterTrainer = "MasterTrainer";

    /// <summary>
    /// İşletme insan kaynakları (#172) — işletme adına belge/sağlık raporu tarayıp girer ve
    /// devam kaydı tutar. <see cref="CompanyManager"/>'ın geniş demetini ALMAZ: öğrenci talebi,
    /// dekont yükleme, işletme belge yönetimi ve not girişi yöneticide kalır.
    /// Girdiği hiçbir kayıt doğrudan hüküm doğurmaz — <c>attendance:direct-entry</c> ve
    /// <c>attendance:health-report:direct</c> izinleri bu rolde YOKTUR.
    /// </summary>
    public const string CompanyHR = "CompanyHR";

    /// <summary>
    /// Sistem yöneticisi (#147) — ULUSAL parametreleri girer (asgari ücret, 3308 oranları).
    /// Kurum verisine yetkisi YOKTUR; okul rollerinin de ulusal parametreye yetkisi yoktur.
    /// Gerçek işletimde bu işi Bakanlık düzeyi bir aktör yapar; o aktör tanımlandığında aynı
    /// <c>platform:</c> izinlerini alır ve bu rol yerini ona bırakır.
    /// </summary>
    public const string SystemAdmin = "SystemAdmin";

    /// <summary>Rol kataloğu: ad + Türkçe etiket + açıklama. Arayüz etiketleri buradan gelir.</summary>
    public static IReadOnlyList<MesnetRoleInfo> Catalog { get; } =
    [
        new(InstitutionManager, "Kurum Müdürü",
            "Kurumdaki tüm süreçleri, personeli ve kullanıcıları yönetir."),
        new(DeputyDirector, "Müdür Yardımcısı",
            "Staj işlemlerini koordine eder; evrak, öğretmen görevlendirme, dekont ve maaş süreçlerini yürütür."),
        new(InstitutionStaff, "Kurum Personeli",
            "Öğrenci kayıt işlemleri, belge doğrulama, devamsızlık takibi ve maaş hesaplamalarını yapar."),
        new(DepartmentHead, "Alan Şefi",
            "Alanındaki işletme dağıtımını, öğretmen atamalarını ve iş yükünü yönetir."),
        new(Teacher, "Koordinatör Öğretmen",
            "İşletme ziyaretleri, öğrenci takibi, rapor ve dekont onayı süreçlerini yürütür."),
        new(CompanyManager, "İşletme Yetkilisi",
            "İşletmedeki stajyerleri, belgeleri, devamsızlığı ve dekontları yönetir."),
        new(MasterTrainer, "Usta Öğretici",
            "İşletmedeki öğrencilerin devam takibini ve dönem not girişini yapar."),
        new(CompanyHR, "İşletme İnsan Kaynakları",
            "İşletme adına belge ve sağlık raporu girer, devam kaydı tutar; girişleri okul onayına tabidir."),
        new(Student, "Öğrenci",
            "Kendi staj, devamsızlık ve ödeme bilgilerini görüntüler."),
        new(SystemAdmin, "Sistem Yöneticisi",
            "Ulusal hesaplama parametrelerini (asgari ücret, 3308 oranları) girer; kurum verisine yetkisi yoktur."),
    ];

    public static IReadOnlyList<string> All { get; } = [.. Catalog.Select(r => r.Name)];

    /// <summary>Verilen ad sistemdeki bir realm rolü mü? Karşılaştırma büyük/küçük harfe duyarsızdır.</summary>
    public static bool IsValid(string? role) =>
        role is not null && All.Contains(role, StringComparer.OrdinalIgnoreCase);

    /// <summary>Rol kataloğu kaydını döndürür; tanınmayan ad için <c>null</c>.</summary>
    public static MesnetRoleInfo? Find(string? role) =>
        role is null
            ? null
            : Catalog.FirstOrDefault(r => string.Equals(r.Name, role, StringComparison.OrdinalIgnoreCase));
}
