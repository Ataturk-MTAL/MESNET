namespace MESNET.Common.Shared.Security;

/// <summary>
/// Birden çok izinden <b>herhangi birini</b> kabul eden policy adları (#182).
///
/// <para>Bazı listeleme uçları hem okul tarafına hem <b>veri sahibine</b> açıktır: devamsızlık
/// listesini okul <c>attendance:view</c> ile, öğrenci ve veli <c>attendance:view-own</c> ile
/// görür. Bu izinler tanımlıydı ama <b>hiçbir uçta kullanılmıyordu</b> — sonuç: öğrenci kendi
/// devamsızlığını, veli de öğrencisininkini hiç göremiyordu.</para>
///
/// <para><b>Neden tek uç, iki ayrı uç değil:</b> ayrı uç aynı sorguyu iki kez yazdırır ve
/// kapsam kuralını iki yerde tutar; biri güncellenip diğeri unutulduğunda sapma sessiz olur.
/// Tek uç + <b>kapsam merdiveni</b> deseni zaten kodda mevcut
/// (<c>ListPaidLeaveRequestsHandler</c>, #177).</para>
///
/// <para><b>Erişim ile kapsam ayrıdır</b> (ADR-0001): policy yalnız ucun açılıp açılmayacağına
/// karar verir. <c>view-own</c> ile giren kullanıcı ucu açar ama <b>yalnız kendi kapsamını</b>
/// görür — daraltmayı handler yapar.</para>
/// </summary>
public static class PermissionPolicies
{
    /// <summary>Policy adındaki izinleri ayıran işaret. Ad okunabilir kalsın diye metin.</summary>
    public const string Separator = "|";

    /// <summary>Devamsızlık listesi — okul tarafı ya da veri sahibi (öğrenci/veli).</summary>
    public static readonly string AttendanceViewOrOwn =
        AnyOf(Permissions.Attendance.View, Permissions.Attendance.ViewOwn);

    /// <summary>Staj listesi/durumu — okul tarafı ya da veri sahibi.</summary>
    public static readonly string InternshipViewOrOwn =
        AnyOf(Permissions.Internship.View, Permissions.Internship.ViewOwn);

    /// <summary>Ücret/dekont listesi — okul tarafı ya da veri sahibi.</summary>
    public static readonly string SalaryViewOrOwn =
        AnyOf(Permissions.Salary.View, Permissions.Salary.ViewOwn);

    /// <summary>
    /// Kullanıcının kurum bağını değiştirme ucu — normal kapsam yolu ya da müdahale yolu
    /// (B parçası, <c>InstitutionBootstrapPolicy</c>).
    ///
    /// <para><b>Neden gerekli:</b> il/ilçe yetkilisi rollerinde (<c>ProvincialAdmin</c>,
    /// <c>DistrictAdmin</c>) <c>user:roles:manage</c> YOKTUR — kasıtlı olarak, o izin alt
    /// ağaçtaki her okulda her kullanıcının rollerini değiştirmek demektir ve istenen şeyden
    /// kat kat geniştir. Yalnız <c>user:roles:manage</c> ile korunan bir uç bu rolleri hiç
    /// içeri almazdı ve müdahale yolu handler'a hiç ulaşmazdı. Asıl daraltma (tıkanıklık var
    /// mı, hedef alt ağaçta mı) yine <c>ChangeUserInstitutionHandler</c>'da yapılır — bu
    /// policy yalnız ucu açar.</para>
    /// </summary>
    public static readonly string UserInstitutionAssignOrBootstrap =
        AnyOf(Permissions.UserManagement.RolesManage, Permissions.Directorate.InstitutionBootstrap);

    /// <summary>
    /// Tıkanma eşiği (staj onay yapılandırması) okuma — müdahale yetkisi ya da ulusal
    /// parametre yönetimi.
    ///
    /// <para><b>Neden iki izin birden:</b> eşiği <b>yazan</b> tek rol <c>SystemAdmin</c>'dir
    /// ve yalnız <c>platform:parameter:manage</c> taşır — <c>internship:approval:override</c>
    /// ona verilmez, çünkü bu izin <c>SubtreeTenantScope</c>'ta <c>Unrestricted</c> dalını da
    /// açar (ulusal okuma) ve istenen bu değildir. Ama okuma ucu yalnız
    /// <c>internship:approval:override</c> ile korunursa yazan rol kendi yazdığı değeri
    /// okuyamaz — yönetim sayfası eşiği boş render eder. Bu policy iki tarafı da kabul eder:
    /// biri sayıyı kullanır (müdahale eden), diğeri sayıyı belirler (SystemAdmin).</para>
    /// </summary>
    public static readonly string ApprovalConfigView =
        AnyOf(Permissions.Internship.ApprovalOverride, Permissions.Platform.ParameterManage);

    /// <summary>Kayıtlı tüm birleşik policy'ler — DI kaydı buradan okur, elle liste tutulmaz.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        AttendanceViewOrOwn,
        InternshipViewOrOwn,
        SalaryViewOrOwn,
        UserInstitutionAssignOrBootstrap,
        ApprovalConfigView
    ];

    /// <summary>Policy adını izinlerine ayırır.</summary>
    public static string[] Split(string policyName) =>
        policyName.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Bir policy adı birleşik mi (yoksa tek izin mi).</summary>
    public static bool IsCombined(string policyName) => policyName.Contains(Separator, StringComparison.Ordinal);

    private static string AnyOf(params string[] permissions) => string.Join(Separator, permissions);
}
