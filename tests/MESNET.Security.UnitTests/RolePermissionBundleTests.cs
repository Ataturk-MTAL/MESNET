using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Yeni rollerin izin demetleri (#129) — <b>kapsam kilidi</b>.
///
/// <para>Demetler actors.md'deki aktör sorumluluklarından türetilmiştir. Genişletmek kolay ve
/// sessizdir; bu testler genişlemeyi görünür kılar.</para>
/// </summary>
public sealed class RolePermissionBundleTests
{
    private static IReadOnlyList<string> PermissionsOf(string role) =>
        RolePermissionMap.GetPermissionsForRoles([role]);

    // ── MasterTrainer: bilinçli olarak DAR ───────────────────────────────────────────────

    [Fact]
    public void Usta_ogretici_devam_ve_not_girisi_yapar()
    {
        var permissions = PermissionsOf(MesnetRoles.MasterTrainer);

        permissions.ShouldContain(Permissions.Company.Attendance);
        permissions.ShouldContain(Permissions.Company.EnterGrade);
        permissions.ShouldContain(Permissions.Student.View);
        permissions.ShouldContain(Permissions.Communication.SendMessage);
        permissions.ShouldContain(Permissions.Communication.ViewMessages);
    }

    /// <summary>
    /// Usta öğretici işletmenin <b>yönetiminden</b> sorumlu değildir: öğrenci talebi, dekont
    /// yükleme ve işletme belge yönetimi <c>CompanyManager</c>'da kalır. Demeti genişletmek
    /// isteyen biri bu testi kırmadan yapamaz.
    /// </summary>
    [Theory]
    [InlineData("company:student:request")]
    [InlineData("company:receipt:upload")]
    [InlineData("company:document:manage")]
    [InlineData("company:manage")]
    [InlineData("company:trainer:manage")]
    public void Usta_ogretici_isletme_yonetim_izinlerini_almaz(string permission)
    {
        PermissionsOf(MesnetRoles.MasterTrainer).ShouldNotContain(permission);
    }

    [Fact]
    public void Usta_ogretici_isletme_yoneticisinin_demetini_almaz()
    {
        // Kaba kontrol: dar demet, geniş demetin alt kümesi olmalı ve ondan belirgin biçimde küçük.
        var trainer = PermissionsOf(MesnetRoles.MasterTrainer);
        var manager = PermissionsOf(MesnetRoles.CompanyManager);

        trainer.Count.ShouldBeLessThan(manager.Count);
    }

    // ── DeputyDirector: müdür yardımcısı yetkileri ───────────────────────────────────────

    [Theory]
    [InlineData("user:roles:manage")]      // davet onayı / kullanıcı yönetimi
    [InlineData("user:approve")]
    [InlineData("department:distribution:manage")] // öğretmen görevlendirme + dağıtım
    [InlineData("internship:approve")]
    [InlineData("internship:contract:manage")]
    [InlineData("document:approve")]       // evrak onayı
    [InlineData("salary:approve")]         // dekont onay zinciri
    [InlineData("salary:parameter:view")]  // asgari ücreti GÖRÜR (#147)
    [InlineData("attendance:approve")]
    public void Mudur_yardimcisi_koordinasyon_ve_onay_yetkilerini_alir(string permission)
    {
        PermissionsOf(MesnetRoles.DeputyDirector).ShouldContain(permission);
    }

    [Fact]
    public void Mudur_yardimcisi_asgari_ucreti_gorur_ama_DEGISTIREMEZ()
    {
        // #147: parametre ulusal mevzuattır. Bu izin eskiden "salary:parameter:manage" idi ve
        // salary:* ile buraya geliyordu; yazma artık platform: önekinde ve yalnız SystemAdmin'de.
        var permissions = PermissionsOf(MesnetRoles.DeputyDirector);

        permissions.ShouldContain(Permissions.Salary.ParameterView);
        permissions.ShouldNotContain(Permissions.Platform.ParameterManage);
    }

    // ── InstitutionStaff: yürütür, onaylamaz ─────────────────────────────────────────────

    [Theory]
    [InlineData("student:manage")]         // öğrenci kayıt işlemleri
    [InlineData("document:verify")]        // belge doğrulama
    [InlineData("attendance:manage")]      // devamsızlık takibi
    [InlineData("salary:calculate")]       // maaş hesaplamaları
    public void Kurum_personeli_actors_md_sorumluluklarini_alir(string permission)
    {
        PermissionsOf(MesnetRoles.InstitutionStaff).ShouldContain(permission);
    }

    /// <summary>
    /// <b>#129 daraltması.</b> Eski <c>InstitutionStaff</c> demeti gerçekte müdür yardımcısının
    /// demetiydi (<c>user:*</c>, <c>department:*</c>, tüm onaylar) ve <c>DeputyDirector</c>'e
    /// taşındı. Geri sızarsa, personel farkında olmadan yönetici yetkisi kazanır.
    /// </summary>
    [Theory]
    [InlineData("user:roles:manage")]
    [InlineData("user:create")]
    [InlineData("department:distribution:manage")]
    [InlineData("internship:approve")]
    [InlineData("document:approve")]
    [InlineData("salary:approve")]
    [InlineData("salary:parameter:manage")]
    [InlineData("attendance:approve")]
    [InlineData("company:manage")]
    public void Kurum_personeli_yonetici_yetkilerini_almaz(string permission)
    {
        PermissionsOf(MesnetRoles.InstitutionStaff).ShouldNotContain(permission);
    }

    // ── Alan (branş) zorunluluğu, yeni rollerde ──────────────────────────────────────────

    [Theory]
    [InlineData(MesnetRoles.DeputyDirector)]   // muafiyeti var → alan istenmez
    [InlineData(MesnetRoles.InstitutionStaff)] // dağıtım izni yok → alan istenmez
    [InlineData(MesnetRoles.MasterTrainer)]
    public void Yeni_rollerde_alan_kodu_zorunlu_degildir(string role)
    {
        BranchRequirement.IsRequiredForRoles([role]).ShouldBeFalse();
    }

    [Fact]
    public void Alan_sefinde_alan_kodu_hala_zorunludur()
    {
        BranchRequirement.IsRequiredForRoles([MesnetRoles.DepartmentHead]).ShouldBeTrue();
    }
}
