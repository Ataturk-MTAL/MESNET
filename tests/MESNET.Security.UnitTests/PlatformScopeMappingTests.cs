using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Ulusal (platform) izninin rol haritasındaki yeri (#147) — <b>wildcard tuzağı</b> regresyonu.
///
/// <para><c>InstitutionManager</c> hem <c>institution:*</c> hem <c>salary:*</c> wildcard'ını
/// tutuyor. Ulusal yazma izni <c>salary:national:manage</c> ya da <c>institution:...</c> diye
/// adlandırılsaydı wildcard onu her okul müdürüne verir ve ulusal/kurum ayrımı sessizce hiç
/// çalışmazdı — #126'daki muafiyet-öneki tuzağının birebir tekrarı. Aşağıdaki testler ayrımı
/// kilitler: adı ya da haritayı değiştiren biri kırmızı testle öğrenir.</para>
/// </summary>
public sealed class PlatformScopeMappingTests
{
    /// <summary>Ulusal parametreye yetkisi OLMAMASI gereken roller.</summary>
    public static TheoryData<string> SchoolRoles =>
    [
        MesnetRoles.InstitutionManager,
        MesnetRoles.DeputyDirector,
        MesnetRoles.InstitutionStaff,
        MesnetRoles.DepartmentHead,
        MesnetRoles.Teacher,
        MesnetRoles.CompanyManager,
        MesnetRoles.MasterTrainer,
        MesnetRoles.CompanyHR,
        MesnetRoles.Student
    ];

    private static IReadOnlyList<string> PermissionsOf(string role) =>
        RolePermissionMap.GetPermissionsForRoles([role]);

    [Fact]
    public void Ulusal_izin_okul_rollerinin_wildcard_oneklerinden_hicbirini_kullanmaz()
    {
        // Bu önekler bir ya da daha fazla okul rolünde wildcard olarak duruyor; ulusal izin
        // bunlardan birinin altında olamaz.
        foreach (var swallowingPrefix in new[]
                 {
                     "institution:", "salary:", "student:", "internship:", "attendance:",
                     "document:", "communication:", "user:", "coordinator:", "department:",
                     "company:"
                 })
        {
            Permissions.Platform.ParameterManage.ShouldNotStartWith(swallowingPrefix);
        }
    }

    [Theory]
    [MemberData(nameof(SchoolRoles))]
    public void Hicbir_okul_rolu_ulusal_parametreyi_yazamaz(string role)
    {
        PermissionsOf(role).ShouldNotContain(Permissions.Platform.ParameterManage);
    }

    [Theory]
    [MemberData(nameof(SchoolRoles))]
    public void Hicbir_okul_rolu_platform_onekli_izin_almaz(string role)
    {
        // Yalnız bilinen izin adını değil, önekin tamamını kilitler: ileride eklenen bir
        // platform izni sessizce okul rolüne düşerse bu test kırılır.
        PermissionsOf(role).ShouldNotContain(p => p.StartsWith("platform:", StringComparison.Ordinal));
    }

    [Fact]
    public void Sistem_yoneticisi_ulusal_parametreyi_yazar()
    {
        PermissionsOf(MesnetRoles.SystemAdmin)
            .ShouldContain(Permissions.Platform.ParameterManage);
    }

    [Fact]
    public void Okul_rolleri_ulusal_parametreyi_GORUR()
    {
        // Yazma kapalı, okuma açık: müdür hangi asgari ücretin yürürlükte olduğunu görmeli.
        PermissionsOf(MesnetRoles.InstitutionManager)
            .ShouldContain(Permissions.Salary.ParameterView);
        PermissionsOf(MesnetRoles.DeputyDirector)
            .ShouldContain(Permissions.Salary.ParameterView);
    }

    [Fact]
    public void Sistem_yoneticisi_kurum_verisine_yetkili_degildir()
    {
        var permissions = PermissionsOf(MesnetRoles.SystemAdmin);

        // Ulusal rol kurum verisi görmez; tek istisna yazdığı parametrenin geçmişi.
        permissions.ShouldNotContain(Permissions.Salary.View);
        permissions.ShouldNotContain(Permissions.Student.View);
        permissions.ShouldNotContain(Permissions.Attendance.View);
        permissions.ShouldNotContain(Permissions.Institution.View);
        permissions.ShouldNotContain(Permissions.Company.View);
    }

    [Fact]
    public void Ulusal_izin_bireysel_atanamaz()
    {
        // InstitutionManager'ın atanabilir kapsamı "*"; bu liste olmasaydı ulusal izni
        // istediği kullanıcıya bireysel atayıp ayrımı tümden kaldırabilirdi.
        AssignablePermissionScope.NeverDirectlyAssignable
            .ShouldContain(Permissions.Platform.ParameterManage);

        AssignablePermissionScope.CanAssign(
            AssignablePermissionScope.Defaults,
            [MesnetRoles.InstitutionManager],
            Permissions.Platform.ParameterManage).ShouldBeFalse();
    }

    [Fact]
    public void Sistem_yoneticisi_bile_ulusal_izni_bireysel_atayamaz()
    {
        AssignablePermissionScope.CanAssign(
            AssignablePermissionScope.Defaults,
            [MesnetRoles.SystemAdmin],
            Permissions.Platform.ParameterManage).ShouldBeFalse();
    }

    [Fact]
    public void Eski_yazma_izni_adi_artik_tanimli_degil()
    {
        // "salary:parameter:manage" kaldırıldı; kalsaydı salary:* ile her müdüre geçer ve
        // uçlardan biri yanlışlıkla ona bağlanabilirdi.
        Permissions.GetAll().ShouldNotContain("salary:parameter:manage");
    }
}
