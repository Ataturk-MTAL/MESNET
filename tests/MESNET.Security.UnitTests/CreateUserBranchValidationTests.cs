using MESNET.Common.Shared.Security;
using MESNET.Security.Application.Commands;
using MESNET.Security.Application.Validators;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Kullanıcı kaydında alan (branş) doğrulaması (#126).
///
/// <para>Alan bilgisi <b>kayıt sırasında</b> sabitlenir. Dağıtım yetkisi olan ama kurum
/// geneli muafiyeti bulunmayan bir kullanıcı alansız kaydedilirse, oluşturulduğu anda
/// hiçbir alana yazamaz hâle gelir — bu yüzden doğrulama kayıtta yapılır.</para>
///
/// <para>Muafiyeti olanlarda (müdür, müdür yardımcısı) alan İSTENMEZ: bu kişiler hiçbir
/// alana bağlı değildir ve boş liste onlarda doğru durumdur.</para>
/// </summary>
public sealed class CreateUserBranchValidationTests
{
    private static readonly CreateUserValidator Validator = new();

    private static CreateUser Command(IEnumerable<string> roles, List<string>? branchCodes = null) =>
        new(
            Username: "test.kullanici",
            Email: "test@mesnet.local",
            FirstName: "Test",
            LastName: "Kullanıcı",
            TemporaryPassword: null,
            Roles: [.. roles],
            BranchCodes: branchCodes);

    [Fact]
    public void Alan_sefi_bransiz_olusturulamaz()
    {
        var result = Validator.Validate(Command([MesnetRoles.DepartmentHead]));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.PropertyName == nameof(CreateUser.BranchCodes));
    }

    [Fact]
    public void Alan_sefi_bos_liste_ile_de_olusturulamaz()
    {
        Validator.Validate(Command([MesnetRoles.DepartmentHead], []))
            .IsValid.ShouldBeFalse();

        // Yalnız boşluk içeren kod da geçerli sayılmaz
        Validator.Validate(Command([MesnetRoles.DepartmentHead], ["   "]))
            .IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Alan_sefi_bransla_olusturulabilir()
    {
        Validator.Validate(Command([MesnetRoles.DepartmentHead], ["EET"]))
            .IsValid.ShouldBeTrue();
    }

    [Fact]
    public void Birden_cok_alandan_sorumlu_alan_sefi_olusturulabilir()
    {
        Validator.Validate(Command([MesnetRoles.DepartmentHead], ["EET", "MTT"]))
            .IsValid.ShouldBeTrue();
    }

    /// <summary>Senaryonun kalbi: yöneticinin branşı yoktur ve bu doğru durumdur.</summary>
    [Theory]
    [InlineData(MesnetRoles.InstitutionManager)]
    [InlineData(MesnetRoles.InstitutionStaff)]
    public void Muafiyetli_yonetici_bransiz_olusturulabilir(string role)
    {
        Validator.Validate(Command([role])).IsValid.ShouldBeTrue();
        Validator.Validate(Command([role], [])).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData(MesnetRoles.Teacher)]
    [InlineData(MesnetRoles.Student)]
    [InlineData(MesnetRoles.CompanyManager)]
    public void Dagitim_yetkisi_olmayan_roller_bransiz_olusturulabilir(string role)
    {
        Validator.Validate(Command([role])).IsValid.ShouldBeTrue();
    }
}
