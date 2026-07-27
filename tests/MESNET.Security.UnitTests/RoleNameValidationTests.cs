using MESNET.Common.Shared.Security;
using MESNET.Security.Application.Commands;
using MESNET.Security.Application.Errors;
using MESNET.Security.Application.Validators;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Rol adı sunucu doğrulaması (#129).
///
/// <para>Arayüzün rol listesini API'den alması <b>tek başına güvence değildir</b>: istek doğrudan
/// atılabilir, eski istemci önbellekte kalabilir, seeder yanlış ad yazabilir. Tanınmayan ad
/// sınırda reddedilmezse Keycloak'ta çözülemez ve kullanıcı sıfır realm rolüyle — hiçbir izin
/// almadan, hiçbir hata görmeden — açılır. Asıl güvence budur, UI senkronu değil.</para>
/// </summary>
public sealed class RoleNameValidationTests
{
    private static readonly string InvalidRoleCode = SecurityErrors.InvalidRole(string.Empty).Code;

    private static CreateInvitation Invitation(string targetRole) =>
        new(
            Email: "davet@mesnet.local",
            FirstName: "Test",
            LastName: "Kullanıcı",
            TargetRole: targetRole,
            CreatedByName: "Test Yönetici");

    private static CreateUser User(params string[] roles) =>
        new(
            Username: "test.kullanici",
            Email: "test@mesnet.local",
            FirstName: "Test",
            LastName: "Kullanıcı",
            TemporaryPassword: null,
            Roles: [.. roles],
            BranchCodes: ["EET"]);

    // ── Davet ────────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("deputy_director")]
    [InlineData("coordinator_teacher")]
    [InlineData("master_trainer")]
    [InlineData("institution_manager")]
    [InlineData("Yonetici")]
    public void Gecersiz_rol_adiyla_davet_olusturulamaz(string role)
    {
        var result = new CreateInvitationValidator().Validate(Invitation(role));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == InvalidRoleCode);
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains(role));
    }

    [Theory]
    [InlineData(MesnetRoles.DeputyDirector)]
    [InlineData(MesnetRoles.MasterTrainer)]
    [InlineData(MesnetRoles.InstitutionStaff)]
    [InlineData(MesnetRoles.Teacher)]
    public void Gecerli_rol_adiyla_davet_olusturulabilir(string role)
    {
        new CreateInvitationValidator().Validate(Invitation(role)).IsValid.ShouldBeTrue();
    }

    /// <summary>Boş rol "geçersiz rol" değil "eksik alan"dır — mesaj ona göre olmalı.</summary>
    [Fact]
    public void Bos_hedef_rol_zorunlu_alan_hatasi_verir()
    {
        var result = new CreateInvitationValidator().Validate(Invitation(string.Empty));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == "Hedef rol belirtilmelidir.");
    }

    // ── Rol değiştirme ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Gecersiz_rol_adiyla_rol_degistirilemez()
    {
        var result = new ChangeUserRolesValidator()
            .Validate(new ChangeUserRoles(Guid.NewGuid(), ["deputy_director"]));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == InvalidRoleCode);
    }

    /// <summary>Karışık liste de reddedilir — geçerli roller "kısmen uygula" gerekçesi değildir.</summary>
    [Fact]
    public void Karisik_listede_tek_gecersiz_rol_bile_reddedilir()
    {
        var result = new ChangeUserRolesValidator()
            .Validate(new ChangeUserRoles(Guid.NewGuid(), [MesnetRoles.Teacher, "master_trainer"]));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("master_trainer"));
        result.Errors.ShouldNotContain(e => e.ErrorMessage.Contains(MesnetRoles.Teacher));
    }

    [Fact]
    public void Gecerli_rollerle_rol_degistirilebilir()
    {
        new ChangeUserRolesValidator()
            .Validate(new ChangeUserRoles(Guid.NewGuid(), [MesnetRoles.DeputyDirector, MesnetRoles.Teacher]))
            .IsValid.ShouldBeTrue();
    }

    // ── Kullanıcı oluşturma ──────────────────────────────────────────────────────────────

    [Fact]
    public void Gecersiz_rol_adiyla_kullanici_olusturulamaz()
    {
        var result = new CreateUserValidator().Validate(User("coordinator_teacher"));

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorCode == InvalidRoleCode);
    }

    [Fact]
    public void Gecerli_rol_adiyla_kullanici_olusturulabilir()
    {
        new CreateUserValidator().Validate(User(MesnetRoles.MasterTrainer)).IsValid.ShouldBeTrue();
    }
}
