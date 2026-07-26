using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Coordination.UnitTests;

/// <summary>
/// "Bu kullanıcıya alan (branş) girilmesi zorunlu mu?" kararı (#126).
///
/// <para>Alan bilgisi <b>kayıt sırasında</b> sabitlenir; sistem tahmin etmez. Zorunluluk
/// rol adından değil permission'dan türetilir — yeni bir unvan aynı yetkiyi aldığında
/// koda dokunmadan doğru davranış oluşsun diye.</para>
/// </summary>
public sealed class BranchRequirementTests
{
    [Fact]
    public void Alan_sefi_icin_alan_zorunludur()
    {
        // Dağıtım izni var, kurum geneli muafiyeti yok → en az bir alan gerekir
        BranchRequirement.IsRequiredForRoles([MesnetRoles.DepartmentHead]).ShouldBeTrue();
    }

    /// <summary>
    /// Okul müdürü ve müdür yardımcısı hiçbir alana bağlı değildir; alan istenmez ve
    /// boş bırakılabilir. Zorunlu kılınsaydı yönetici kaydı hiç açılamazdı.
    /// </summary>
    [Theory]
    [InlineData(MesnetRoles.InstitutionManager)]
    [InlineData(MesnetRoles.InstitutionStaff)]
    public void Kurum_geneli_yetkili_roller_icin_alan_zorunlu_degildir(string role)
    {
        BranchRequirement.IsRequiredForRoles([role]).ShouldBeFalse();
    }

    [Theory]
    [InlineData(MesnetRoles.Teacher)]
    [InlineData(MesnetRoles.Student)]
    [InlineData(MesnetRoles.CompanyManager)]
    public void Dagitim_yetkisi_olmayan_roller_icin_alan_zorunlu_degildir(string role)
    {
        BranchRequirement.IsRequiredForRoles([role]).ShouldBeFalse();
    }

    /// <summary>
    /// Alan şefi aynı zamanda müdür yardımcısıysa muafiyet kazanır → alan istenmez.
    /// Muafiyet her zaman zorunluluğu iptal eder.
    /// </summary>
    [Fact]
    public void Muafiyetli_rol_eklendiginde_zorunluluk_kalkar()
    {
        BranchRequirement
            .IsRequiredForRoles([MesnetRoles.DepartmentHead, MesnetRoles.InstitutionStaff])
            .ShouldBeFalse();
    }

    [Fact]
    public void Rol_yoksa_alan_zorunlu_degildir()
    {
        BranchRequirement.IsRequiredForRoles([]).ShouldBeFalse();
        BranchRequirement.IsRequiredForRoles(null).ShouldBeFalse();
    }

    /// <summary>
    /// Doğrudan (direct) atanmış yetkiler de karara girer: rolü olmasa da dağıtım izni
    /// bireysel olarak verilmiş bir kullanıcı alansız bırakılamaz.
    /// </summary>
    [Fact]
    public void Bireysel_dagitim_izni_de_alani_zorunlu_kilar()
    {
        BranchRequirement
            .IsRequiredForPermissions([Permissions.DepartmentHead.Distribution])
            .ShouldBeTrue();

        BranchRequirement
            .IsRequiredForPermissions(
                [Permissions.DepartmentHead.Distribution, Permissions.Institution.AllBranches])
            .ShouldBeFalse();
    }
}
