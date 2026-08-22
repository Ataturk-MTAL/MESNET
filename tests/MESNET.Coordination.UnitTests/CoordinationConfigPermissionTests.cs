using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Coordination.UnitTests;

/// <summary>
/// Kurum geneli koordinasyon yapılandırması izninin rol haritasındaki yeri (#130).
///
/// <para>#126 koordinasyon <b>yazma</b> uçlarına alan kapsamı kontrolü getirdi, ama
/// <c>CoordinationConfig</c> alan bazlı olmadığı için kapsam dışında kaldı. Alan şefi
/// doğrudan yazamadığı alanları kurum geneli parametreyi değiştirerek dolaylı
/// etkileyebiliyordu: <c>MaxWeeklyExtraHours</c> düşünce o alanların mevcut atamaları limit
/// üstüne çıkar, mesafe kuralları değişince tüm alanların <c>MaxCoordinationHours</c>
/// tavanları ve #116 dağıtım önerileri kayar.</para>
///
/// <para>Çözüm ayrı ve kesin bir izindir:
/// <see cref="Permissions.Institution.CoordinationConfigManage"/>. Aşağıdaki testler onu
/// yerinde tutar — özellikle <b>wildcard tuzağını</b>: <c>DepartmentHead</c> rolü
/// <c>department:*</c> taşır, izin o önekte olsaydı kısıt sessizce hiç çalışmazdı.</para>
/// </summary>
public sealed class CoordinationConfigPermissionTests
{
    private static IReadOnlyList<string> PermissionsOf(string role) =>
        RolePermissionMap.GetPermissionsForRoles([role]);

    [Fact]
    public void Yapilandirma_izni_department_onekiyle_baslamaz()
    {
        // "department:*" wildcard'ı DepartmentHead'de var; izin o önekte olamaz.
        Permissions.Institution.CoordinationConfigManage.ShouldNotStartWith("department:");
        Permissions.Institution.CoordinationConfigManage.ShouldStartWith("institution:");
    }

    /// <summary>
    /// <b>Asıl kilit.</b> Alan şefi izni hiçbir yoldan almamalı — ne açık kayıtla ne de
    /// <c>department:*</c> wildcard'ı genişletilirken.
    /// </summary>
    [Fact]
    public void Alan_sefi_yapilandirma_iznini_almaz()
    {
        PermissionsOf(MesnetRoles.DepartmentHead)
            .ShouldNotContain(Permissions.Institution.CoordinationConfigManage);
    }

    /// <summary>
    /// Kısıt erişimi kaldırmaz: alan şefi koordinasyon dağıtımını yönetmeye devam eder,
    /// yalnız kurum geneli parametreye dokunamaz.
    /// </summary>
    [Fact]
    public void Alan_sefi_dagitim_iznini_almaya_devam_eder()
    {
        PermissionsOf(MesnetRoles.DepartmentHead)
            .ShouldContain(Permissions.DepartmentHead.Distribution);
    }

    /// <summary>
    /// Yapılandırma kurum düzeyi bir ayardır: okul müdürü ve müdür yardımcısı değiştirir.
    /// <c>InstitutionManager</c> <c>institution:*</c> ile, <c>DeputyDirector</c> açık kayıtla alır.
    /// </summary>
    [Theory]
    [InlineData(MesnetRoles.InstitutionManager)]
    [InlineData(MesnetRoles.DeputyDirector)]
    public void Kurum_duzeyi_yetkili_roller_yapilandirma_iznini_alir(string role)
    {
        PermissionsOf(role).ShouldContain(Permissions.Institution.CoordinationConfigManage);
    }

    /// <summary>
    /// <c>InstitutionStaff</c> ("Kurum Yetkilendirdiği Personel") yürütür, karar vermez —
    /// #129'da koordinasyon dağıtımı ondan alınmıştı; kurum geneli yapılandırma da verilmez.
    /// </summary>
    [Theory]
    [InlineData(MesnetRoles.InstitutionStaff)]
    [InlineData(MesnetRoles.DepartmentHead)]
    [InlineData(MesnetRoles.Teacher)]
    [InlineData(MesnetRoles.Student)]
    [InlineData(MesnetRoles.CompanyManager)]
    [InlineData(MesnetRoles.MasterTrainer)]
    public void Diger_roller_yapilandirma_iznini_almaz(string role)
    {
        PermissionsOf(role).ShouldNotContain(Permissions.Institution.CoordinationConfigManage);
    }

    /// <summary>
    /// İzin katalogda olmalı — aksi hâlde <c>institution:*</c> wildcard'ı genişletilirken
    /// atlanır ve müdür de yetkiyi kaybeder. Ayrıca <c>SecurityServiceExtensions</c> policy'yi
    /// bu katalogdan üretir; katalogda olmayan izin için <c>RequireAuthorization</c> patlar.
    /// </summary>
    [Fact]
    public void Yapilandirma_izni_permission_katalogunda_kayitlidir()
    {
        Permissions.GetAll().ShouldContain(Permissions.Institution.CoordinationConfigManage);
    }

    /// <summary>
    /// İkinci kapı: izin alan şefine <b>bireysel (direct) yetki</b> olarak da verilememeli.
    /// <c>DepartmentHead</c>'in atanabilir domain listesinde <c>institution:</c> yoktur;
    /// <c>department:</c> vardır — izin o önekte olsaydı bir yönetici tek tıkla verebilirdi.
    /// </summary>
    [Fact]
    public void Yapilandirma_izni_alan_sefine_bireysel_olarak_da_atanamaz()
    {
        AssignablePermissionScope.CanAssign(
                AssignablePermissionScope.Defaults,
                [MesnetRoles.DepartmentHead],
                Permissions.Institution.CoordinationConfigManage)
            .ShouldBeFalse();
    }

    /// <summary>
    /// <c>DepartmentHead</c>'in varsayılan atanabilir domain listesinde <c>institution:</c>
    /// bulunmamalıdır — yukarıdaki kapının dayanağı budur, açıkça kilitlenir.
    /// </summary>
    [Fact]
    public void Alan_sefinin_atanabilir_domainlerinde_institution_yoktur()
    {
        AssignablePermissionScope.Defaults[MesnetRoles.DepartmentHead]
            .ShouldNotContain("institution:");
        AssignablePermissionScope.Defaults[MesnetRoles.DepartmentHead]
            .ShouldNotContain(AssignablePermissionScope.All);
    }

    /// <summary>
    /// Bu izin bir <b>erişim</b> iznidir, kapsam muafiyeti DEĞİL — bu yüzden
    /// <see cref="AssignablePermissionScope.NeverDirectlyAssignable"/> listesinde yer almaz.
    /// O liste yalnız "hangi verinin" sorusunun cevabını genişleten izinler içindir (#126).
    /// Kurum düzeyi yetkili bir kullanıcıya bireysel atanabilmesi bilinçlidir.
    /// </summary>
    [Fact]
    public void Yapilandirma_izni_kapsam_muafiyeti_degildir()
    {
        AssignablePermissionScope.NeverDirectlyAssignable
            .ShouldNotContain(Permissions.Institution.CoordinationConfigManage);

        AssignablePermissionScope.CanAssign(
                AssignablePermissionScope.Defaults,
                [MesnetRoles.DeputyDirector],
                Permissions.Institution.CoordinationConfigManage)
            .ShouldBeTrue();
    }

    /// <summary>
    /// Muafiyet izniyle karıştırılmamalı: ikisi ayrı izinlerdir ve biri diğerini getirmez.
    /// Muafiyet "tüm <i>alanlara</i> yazar" demektir; yapılandırmanın alanı yoktur.
    /// </summary>
    [Fact]
    public void Yapilandirma_izni_muafiyet_izninden_ayridir()
    {
        Permissions.Institution.CoordinationConfigManage
            .ShouldNotBe(Permissions.Institution.AllBranches);
    }
}
