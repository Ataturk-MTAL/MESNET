using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// İl/ilçe yetkilisinin izin demeti (B parçası).
///
/// <para><b>Neden `directorate:` diye YENİ bir önek:</b> `institution:` önekli olsaydı
/// <c>InstitutionManager</c>'ın <c>institution:*</c> wildcard'ı üzerinden HER okul müdürüne
/// geçerdi ve okul müdürü kullanıcıları BAŞKA okullara bağlayabilirdi — ADR-0002 önek
/// tuzağının tam kendisi. <c>platform:</c> de kullanılamaz: o önek kurum üstü yetkiyi işaret
/// eder ve il yetkilisine platform yetkisi vermek kapsamı bütün ülkeye açardı.</para>
/// </summary>
public sealed class DirectoratePermissionMappingTests
{
    private static IReadOnlyList<string> PermissionsOf(string role)
        => RolePermissionMap.GetPermissionsForRoles([role]);

    public static TheoryData<string> MudurlukRolleri =>
    [
        MesnetRoles.ProvincialAdmin,
        MesnetRoles.DistrictAdmin,
    ];

    [Theory]
    [MemberData(nameof(MudurlukRolleri))]
    public void Mudurluk_rolleri_kurum_kunyesi_ve_donem_izni_alir(string role)
    {
        PermissionsOf(role).ShouldContain(Permissions.Institution.Manage);
    }

    [Theory]
    [MemberData(nameof(MudurlukRolleri))]
    public void Mudurluk_rolleri_tikanmis_onayi_acabilir(string role)
    {
        PermissionsOf(role).ShouldContain(Permissions.Internship.ApprovalOverride);
    }

    [Theory]
    [MemberData(nameof(MudurlukRolleri))]
    public void Mudurluk_rolleri_okula_ilk_yoneticiyi_baglayabilir(string role)
    {
        PermissionsOf(role).ShouldContain(Permissions.Directorate.InstitutionBootstrap);
    }

    [Theory]
    [MemberData(nameof(MudurlukRolleri))]
    public void Mudurluk_rolleri_onay_zincirinde_NORMAL_ADIM_olamaz(string role)
    {
        // internship:manage müdür onay adımını da açar; istenen yalnız tıkanıklığı açmaktır.
        PermissionsOf(role).ShouldNotContain(Permissions.Internship.Manage);
    }

    [Theory]
    [MemberData(nameof(MudurlukRolleri))]
    public void Mudurluk_rolleri_rol_yonetemez(string role)
    {
        // user:roles:manage alt ağaçtaki her okulda her kullanıcının rollerini değiştirmek
        // demektir — istenen şeyden kat kat geniş.
        PermissionsOf(role).ShouldNotContain(Permissions.UserManagement.RolesManage);
    }

    [Fact]
    public void Bootstrap_izni_baska_HICBIR_role_gitmez()
    {
        foreach (var role in MesnetRoles.All)
        {
            if (role is MesnetRoles.ProvincialAdmin or MesnetRoles.DistrictAdmin) continue;

            PermissionsOf(role).ShouldNotContain(Permissions.Directorate.InstitutionBootstrap,
                $"{role} rolüne bootstrap izni sızmış.");
        }
    }

    [Fact]
    public void Hicbir_rolun_wildcardi_directorate_onekini_yutmaz()
    {
        foreach (var role in MesnetRoles.All)
        {
            RolePermissionMap.GetRawPermissionsForRole(role)
                .ShouldNotContain("directorate:*", $"{role} rolüne directorate: wildcard'ı eklenmiş.");
        }
    }

    [Fact]
    public void Override_iznini_bugun_internship_manage_tasiyan_her_rol_de_alir()
    {
        // GEÇİŞ KAYBI OLMAMALI: ucun izni daraltıldı, kimse yetkisini kaybetmemeli.
        foreach (var role in MesnetRoles.All)
        {
            var izinler = PermissionsOf(role);
            if (!izinler.Contains(Permissions.Internship.Manage)) continue;

            izinler.ShouldContain(Permissions.Internship.ApprovalOverride,
                $"{role} internship:manage taşıyor ama override iznini kaybetti.");
        }
    }

    [Fact]
    public void Yeni_izinler_bireysel_atanamaz()
    {
        AssignablePermissionScope.NeverDirectlyAssignable
            .ShouldContain(Permissions.Directorate.InstitutionBootstrap);
        AssignablePermissionScope.NeverDirectlyAssignable
            .ShouldContain(Permissions.Internship.ApprovalOverride);
    }

    [Fact]
    public void Directorate_oneki_atanabilir_domain_listesinde_YOKTUR()
    {
        AssignablePermissionScope.AllDomains.ShouldNotContain("directorate:");
    }

    [Fact]
    public void Mudurluk_rollerinin_atanabilir_kapsami_BOS_KALIR()
    {
        // A parçasında bilerek boş bırakıldı. Açılırsa il yetkilisi kendi verdiği izinlerle
        // kapsamını genişletir. C yazıldı diye açılmaz — o liste "kime dağıtabilir"
        // sorusudur, "ne yapabilir" değil.
        AssignablePermissionScope.Defaults[MesnetRoles.ProvincialAdmin].ShouldBeEmpty();
        AssignablePermissionScope.Defaults[MesnetRoles.DistrictAdmin].ShouldBeEmpty();
    }
}
