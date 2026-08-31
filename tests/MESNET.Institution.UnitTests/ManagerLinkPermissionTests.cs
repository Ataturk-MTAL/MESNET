using MESNET.Common.Shared.Security;
using MESNET.Institution.Core.ReadModels;
using Shouldly;
using Xunit;

namespace MESNET.Institution.UnitTests;

/// <summary>
/// "Yöneticisi var mı" sorusu <b>izne</b> bakar, rol adına değil (ADR-0001).
///
/// <para>Rol adına bakan bir kontrol yazılsaydı, yeni bir rol (ör. bir müdür vekili rolü)
/// <c>institution:manage</c> taşımasına rağmen listede görünmez ve okul sonsuza kadar
/// "yöneticisiz" kalırdı — hata değil, yanlış liste.</para>
/// </summary>
public sealed class ManagerLinkPermissionTests
{
    [Fact]
    public void Kurum_yoneticisi_rolu_manage_izni_tasir()
    {
        InstitutionManagerLink.HasManage([MesnetRoles.InstitutionManager]).ShouldBeTrue();
    }

    [Fact]
    public void Ogretmen_rolu_manage_izni_tasimaz()
    {
        InstitutionManagerLink.HasManage([MesnetRoles.Teacher]).ShouldBeFalse();
    }

    [Fact]
    public void Rol_yoksa_manage_izni_yoktur()
    {
        InstitutionManagerLink.HasManage([]).ShouldBeFalse();
    }

    [Fact]
    public void Taninmayan_rol_izin_vermez()
    {
        InstitutionManagerLink.HasManage(["BöyleBirRolYok"]).ShouldBeFalse();
    }

    /// <summary>
    /// Rollerden biri yetiyorsa yeter — kullanıcı birden çok rol taşıyabilir.
    /// </summary>
    [Fact]
    public void Rollerden_biri_yetiyorsa_izin_vardir()
    {
        InstitutionManagerLink.HasManage([MesnetRoles.Teacher, MesnetRoles.InstitutionManager]).ShouldBeTrue();
    }
}
