using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Devamsızlık ve sağlık raporu <b>hüküm</b> izinlerinin rol haritasındaki yeri (#172).
///
/// <para>Kural: giriş GENİŞ, hüküm DAR. Sağlık raporunu işletme yetkilisi, işletme İK, usta
/// öğretici ve öğrenci de yükleyebilir; ama girdikleri kayıt koordinatör öğretmen onaylayana
/// kadar devamsızlık türünü değiştirmez — yani ücret kesintisini kaldırmaz. Ödemeyi yapan taraf
/// kendi kesintisini tek taraflı iptal edemez.</para>
///
/// <para>Aşağıdaki testler hem önek tuzağını (ADR-0001) hem de haritayı kilitler.</para>
/// </summary>
public sealed class AttendanceDirectEntryMappingTests
{
    /// <summary>Girdiği kayıt onaya düşmesi gereken taraflar — okul dışı.</summary>
    public static TheoryData<string> NonSchoolRoles =>
    [
        MesnetRoles.CompanyManager,
        MesnetRoles.MasterTrainer,
        MesnetRoles.CompanyHR,
        MesnetRoles.Student,
        // Veli (#174) — girdiği rapor da onaya düşer; ödemeyi etkileyen kararı veremez.
        MesnetRoles.Parent
    ];

    /// <summary>Sahibin saydığı taraf: sağlık raporunu onaysız girebilenler.</summary>
    public static TheoryData<string> HealthReportDirectRoles =>
    [
        MesnetRoles.InstitutionManager,
        MesnetRoles.DeputyDirector,
        MesnetRoles.Teacher
    ];

    /// <summary>Sağlık raporunu yükleyebilmesi gereken taraflar (giriş geniş).</summary>
    public static TheoryData<string> UploadRoles =>
    [
        MesnetRoles.InstitutionManager,
        MesnetRoles.DeputyDirector,
        MesnetRoles.InstitutionStaff,
        MesnetRoles.Teacher,
        MesnetRoles.CompanyManager,
        MesnetRoles.MasterTrainer,
        MesnetRoles.CompanyHR,
        MesnetRoles.Student,
        MesnetRoles.Parent   // #174 — veli de sağlık raporu yükler
    ];

    private static IReadOnlyList<string> PermissionsOf(string role) =>
        RolePermissionMap.GetPermissionsForRoles([role]);

    [Fact]
    public void Hukum_izinleri_isletme_ve_alan_sefi_wildcardlarinin_altinda_degildir()
    {
        // `company:*` CompanyManager'da, `department:*` DepartmentHead ve DeputyDirector'da.
        // İzin bu öneklerden birinde olsaydı işletmeye ya da alan şefine sessizce geçerdi.
        foreach (var swallowingPrefix in new[] { "company:", "department:", "student:" })
        {
            Permissions.Attendance.DirectEntry.ShouldNotStartWith(swallowingPrefix);
            Permissions.Attendance.HealthReportDirect.ShouldNotStartWith(swallowingPrefix);
        }
    }

    [Theory]
    [MemberData(nameof(NonSchoolRoles))]
    public void Okul_disi_rollerin_girdigi_devamsizlik_onaya_duser(string role)
    {
        PermissionsOf(role).ShouldNotContain(Permissions.Attendance.DirectEntry);
    }

    [Theory]
    [MemberData(nameof(NonSchoolRoles))]
    public void Okul_disi_roller_saglik_raporunu_onaysiz_gecerli_kilamaz(string role)
    {
        PermissionsOf(role).ShouldNotContain(Permissions.Attendance.HealthReportDirect);
    }

    [Theory]
    [MemberData(nameof(HealthReportDirectRoles))]
    public void Okul_tarafi_saglik_raporunu_onaysiz_girer(string role)
    {
        PermissionsOf(role).ShouldContain(Permissions.Attendance.HealthReportDirect);
    }

    /// <summary>
    /// Kurum personeli devamsızlığı doğrudan girer (bugünkü davranış korunur, #129) ama sağlık
    /// raporunu onaysız geçerli kılamaz — sahibin saydığı taraf yalnız üç roldür.
    /// </summary>
    [Fact]
    public void Kurum_personeli_devamsizligi_dogrudan_girer_raporu_onaysiz_gecerli_kilamaz()
    {
        var permissions = PermissionsOf(MesnetRoles.InstitutionStaff);

        permissions.ShouldContain(Permissions.Attendance.DirectEntry);
        permissions.ShouldNotContain(Permissions.Attendance.HealthReportDirect);
    }

    [Theory]
    [MemberData(nameof(UploadRoles))]
    public void Saglik_raporu_girisi_genistir(string role)
    {
        PermissionsOf(role).ShouldContain(Permissions.Attendance.Upload);
    }

    /// <summary>
    /// Onay adımı (<c>attendance:approve</c>) işletme tarafında OLMAMALI: yükleyen taraf kendi
    /// yüklediğini onaylayabilseydi zincir hiçbir şey korumazdı.
    /// </summary>
    [Theory]
    [MemberData(nameof(NonSchoolRoles))]
    public void Yukleyen_isletme_tarafi_kendi_raporunu_onaylayamaz(string role)
    {
        PermissionsOf(role).ShouldNotContain(Permissions.Attendance.Approve);
    }

    /// <summary>
    /// Hüküm izinleri bireysel (direct) ASLA atanamaz. İşletme rollerinin atanabilir domain
    /// listesinde <c>attendance:</c> vardır (giriş için gerekli); bu kural olmasaydı müdür
    /// yardımcısı bir işletme yetkilisine <c>attendance:health-report:direct</c>'i atayıp onay
    /// zincirini tümden kaldırabilirdi.
    /// </summary>
    [Fact]
    public void Hukum_izinleri_bireysel_atanamaz()
    {
        AssignablePermissionScope.NeverDirectlyAssignable
            .ShouldContain(Permissions.Attendance.DirectEntry);
        AssignablePermissionScope.NeverDirectlyAssignable
            .ShouldContain(Permissions.Attendance.HealthReportDirect);
    }

    [Theory]
    [MemberData(nameof(NonSchoolRoles))]
    public void Isletme_kullanicisina_hukum_izni_bireysel_atanamaz(string role)
    {
        AssignablePermissionScope.CanAssign(
                AssignablePermissionScope.Defaults, [role], Permissions.Attendance.HealthReportDirect)
            .ShouldBeFalse();

        AssignablePermissionScope.CanAssign(
                AssignablePermissionScope.Defaults, [role], Permissions.Attendance.DirectEntry)
            .ShouldBeFalse();
    }

    /// <summary>
    /// <b>İşletme rolleri tek kişide birleşebilir.</b> Her işletmede ayrı bir İK yoktur; işletme
    /// sahibi aynı zamanda usta öğretici olabilir, ya da sahip/yönetici ile usta öğretici farklı
    /// kişiler olabilir. Kullanıcının izinleri rollerinin BİRLEŞİMİdir — bu yüzden birleşimin de
    /// hüküm izni üretmediği ayrıca kilitlenir. Üretse, "usta öğretici de olan işletme sahibi"
    /// kendi girdiği raporu onaysız geçerli kılar ve kesintiyi kendisi kaldırırdı.
    /// </summary>
    [Theory]
    [InlineData(MesnetRoles.CompanyManager, MesnetRoles.MasterTrainer)]
    [InlineData(MesnetRoles.CompanyManager, MesnetRoles.CompanyHR)]
    [InlineData(MesnetRoles.MasterTrainer, MesnetRoles.CompanyHR)]
    public void Isletme_rolleri_birlestiginde_de_hukum_izni_dogmaz(string first, string second)
    {
        var permissions = RolePermissionMap.GetPermissionsForRoles([first, second]);

        permissions.ShouldContain(Permissions.Attendance.Upload, "İşletme tarafı rapor girebilmeli.");
        permissions.ShouldNotContain(Permissions.Attendance.DirectEntry);
        permissions.ShouldNotContain(Permissions.Attendance.HealthReportDirect);
        permissions.ShouldNotContain(Permissions.Attendance.Approve);
    }

    /// <summary>
    /// Usta öğretici, işletmede İK olmasa bile tek başına sağlık raporu girebilmelidir —
    /// ama girdiği rapor onaya düşer.
    /// </summary>
    [Fact]
    public void Usta_ogretici_tek_basina_rapor_girer_ama_onaya_duser()
    {
        var permissions = PermissionsOf(MesnetRoles.MasterTrainer);

        permissions.ShouldContain(Permissions.Attendance.Upload);
        permissions.ShouldContain(Permissions.Attendance.Manage, "Devamsızlık girişi ucu bu izni ister.");
        permissions.ShouldNotContain(Permissions.Attendance.HealthReportDirect);
        permissions.ShouldNotContain(Permissions.Attendance.Approve);
    }

    /// <summary>Kurum müdürünün "*" kapsamı bile bu izinleri bireysel atatamaz.</summary>
    [Fact]
    public void Kurum_muduru_yildiz_kapsamiyla_bile_hukum_iznini_atayamaz()
    {
        AssignablePermissionScope.CanAssign(
                AssignablePermissionScope.Defaults,
                [MesnetRoles.InstitutionManager],
                Permissions.Attendance.HealthReportDirect)
            .ShouldBeFalse();
    }
}
