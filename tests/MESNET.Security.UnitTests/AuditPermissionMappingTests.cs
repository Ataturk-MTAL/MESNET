using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Denetim izi okuma izninin rol haritasındaki yeri (C parçası).
///
/// <para><b>Neden YENİ bir önek:</b> <c>institution:</c> önekli bir izin
/// <c>InstitutionManager</c>'ın <c>institution:*</c> wildcard'ı üzerinden HER okul müdürüne
/// geçerdi (ADR-0002 önek tuzağı — #126'da alan muafiyeti izninde bire bir yaşandı). Okul
/// müdürünün kendi okulunun izini görmesi İSTENEN bir şeydir, ama kararın wildcard'ın yan
/// etkisiyle değil AÇIKÇA verilmesi gerekir. Bu testler o açıklığı kilitler.</para>
/// </summary>
public sealed class AuditPermissionMappingTests
{
    private static IReadOnlyList<string> PermissionsOf(string role)
        => RolePermissionMap.GetPermissionsForRoles([role]);

    public static TheoryData<string> IzniOlanRoller =>
    [
        MesnetRoles.InstitutionManager,
        MesnetRoles.DeputyDirector,
    ];

    public static TheoryData<string> IzniOlmayanRoller =>
    [
        MesnetRoles.InstitutionStaff,
        MesnetRoles.Teacher,
        MesnetRoles.DepartmentHead,
        MesnetRoles.CompanyManager,
        MesnetRoles.MasterTrainer,
        MesnetRoles.CompanyHR,
        MesnetRoles.Student,
        MesnetRoles.Parent,
        MesnetRoles.ProvincialAdmin,
        MesnetRoles.DistrictAdmin,
        MesnetRoles.SystemAdmin,
    ];

    [Theory]
    [MemberData(nameof(IzniOlanRoller))]
    public void Kurum_denetim_izini_okuyabilen_roller(string role)
    {
        PermissionsOf(role).ShouldContain(Permissions.Audit.ViewInstitution);
    }

    [Theory]
    [MemberData(nameof(IzniOlmayanRoller))]
    public void Diger_hicbir_rol_kurum_denetim_izini_okuyamaz(string role)
    {
        // Wildcard sızıntısının kilidi: "audit:" öneki hiçbir rolün wildcard'ında yok.
        PermissionsOf(role).ShouldNotContain(Permissions.Audit.ViewInstitution);
    }

    [Fact]
    public void Hicbir_rolun_wildcardi_audit_onekini_yutmaz()
    {
        // Doğrudan kaynak taraması: "audit:*" biçiminde bir wildcard eklenirse test kırılır.
        foreach (var role in MesnetRoles.All)
        {
            RolePermissionMap.GetRawPermissionsForRole(role)
                .ShouldNotContain("audit:*", $"{role} rolüne audit: wildcard'ı eklenmiş.");
        }
    }

    [Fact]
    public void Denetim_izni_bireysel_atanamaz()
    {
        // InstitutionManager'ın atanabilir kapsamı "*"tır. Bu koruma olmasaydı okul müdürü
        // denetim görünürlüğünü herhangi bir kullanıcıya — bir İŞLETME kullanıcısına bile —
        // verebilirdi; o kullanıcı okulun bütün eylem günlüğünü okurdu.
        AssignablePermissionScope.NeverDirectlyAssignable
            .ShouldContain(Permissions.Audit.ViewInstitution);
    }

    [Fact]
    public void Audit_oneki_atanabilir_domain_listesinde_YOKTUR()
    {
        AssignablePermissionScope.AllDomains.ShouldNotContain("audit:");
    }
}
