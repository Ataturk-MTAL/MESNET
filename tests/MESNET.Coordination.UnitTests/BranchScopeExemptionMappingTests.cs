using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Coordination.UnitTests;

/// <summary>
/// Muafiyet izninin rol haritasındaki yeri (#126) — <b>wildcard tuzağı</b> regresyonu.
///
/// <para>Üç rolün de <c>department:*</c> wildcard'ı vardır. Muafiyet izni
/// <c>department:distribution:all</c> olarak adlandırılsaydı wildcard onu alan şefine de
/// verir, kapsam kontrolü sessizce hiç çalışmazdı. Bu yüzden izin <c>institution:</c>
/// öneki altındadır. Aşağıdaki testler bu ayrımı kilitler: adı ya da haritayı değiştiren
/// biri, kontrolün kapandığını sessizce değil <b>kırmızı testle</b> öğrenir.</para>
/// </summary>
public sealed class BranchScopeExemptionMappingTests
{
    private static IReadOnlyList<string> PermissionsOf(string role) =>
        RolePermissionMap.GetPermissionsForRoles([role]);

    [Fact]
    public void Muafiyet_izni_department_onekiyle_baslamaz()
    {
        // "department:*" wildcard'ı DepartmentHead'de de var; muafiyet o önekte olamaz.
        Permissions.Institution.AllBranches.ShouldNotStartWith("department:");
    }

    [Fact]
    public void Alan_sefi_muafiyet_iznini_almaz()
    {
        PermissionsOf(MesnetRoles.DepartmentHead)
            .ShouldNotContain(Permissions.Institution.AllBranches);
    }

    [Fact]
    public void Alan_sefi_dagitim_iznini_almaya_devam_eder()
    {
        // Kapsam kontrolü erişimi kaldırmaz: alan şefi hâlâ koordinasyonu yönetir,
        // yalnız kendi alanıyla sınırlıdır.
        PermissionsOf(MesnetRoles.DepartmentHead)
            .ShouldContain(Permissions.DepartmentHead.Distribution);
    }

    [Theory]
    [InlineData(MesnetRoles.InstitutionManager)]
    [InlineData(MesnetRoles.InstitutionStaff)]
    public void Kurum_geneli_yetkili_roller_muafiyet_iznini_alir(string role)
    {
        PermissionsOf(role).ShouldContain(Permissions.Institution.AllBranches);
    }

    [Theory]
    [InlineData(MesnetRoles.Teacher)]
    [InlineData(MesnetRoles.Student)]
    [InlineData(MesnetRoles.CompanyManager)]
    public void Diger_roller_muafiyet_iznini_almaz(string role)
    {
        PermissionsOf(role).ShouldNotContain(Permissions.Institution.AllBranches);
    }

    /// <summary>
    /// Muafiyet izni <c>Permissions.GetAll()</c> içinde olmalı — aksi hâlde
    /// <c>institution:*</c> wildcard'ı genişletilirken atlanır ve müdür de muafiyeti kaybeder.
    /// </summary>
    [Fact]
    public void Muafiyet_izni_permission_kataloğunda_kayitlidir()
    {
        Permissions.GetAll().ShouldContain(Permissions.Institution.AllBranches);
    }

    /// <summary>
    /// İkinci kapı: muafiyet izni alan şefine <b>bireysel (direct) yetki</b> olarak da
    /// atanamamalıdır. <c>DepartmentHead</c>'in atanabilir domain listesinde
    /// <c>institution:</c> yoktur; <c>department:</c> vardır — izin o önekte olsaydı
    /// bir yönetici muafiyeti alan şefine tek tıkla verebilirdi.
    /// </summary>
    [Fact]
    public void Muafiyet_izni_alan_sefine_bireysel_olarak_da_atanamaz()
    {
        AssignablePermissionScope.CanAssign(
                AssignablePermissionScope.Defaults,
                [MesnetRoles.DepartmentHead],
                Permissions.Institution.AllBranches)
            .ShouldBeFalse();
    }

    [Theory]
    [InlineData(MesnetRoles.InstitutionManager)]
    [InlineData(MesnetRoles.InstitutionStaff)]
    public void Kurum_geneli_yetkili_rollere_bireysel_atama_mumkundur(string role)
    {
        AssignablePermissionScope.CanAssign(
                AssignablePermissionScope.Defaults,
                [role],
                Permissions.Institution.AllBranches)
            .ShouldBeTrue();
    }
}
