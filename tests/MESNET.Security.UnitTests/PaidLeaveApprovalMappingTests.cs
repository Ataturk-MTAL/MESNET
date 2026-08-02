using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Ücretli izin onay zincirinin rol haritasındaki yeri (#177).
///
/// <para><b>Bu testlerin asıl konusu izin DEĞİL, kapsamdır.</b> <c>InstitutionManager</c> her
/// domain wildcard'ını taşır — <c>attendance:*</c> dâhil. Yani işletme adımının izni okul
/// müdürüne de gider ve bunu engelleyecek serbest bir önek yoktur (<c>platform:</c> hariç). Bu
/// yüzden testler "müdürde bu izin yok" DEMEZ; wildcard'ın gerçeğini kabul edip zinciri ayakta
/// tutan iki mekanizmayı kilitler: <c>business_id</c> kapsamı (bkz.
/// <c>PaidLeaveApprovalPolicyTests</c>) ve okul adımının bireysel atanamazlığı.</para>
/// </summary>
public sealed class PaidLeaveApprovalMappingTests
{
    /// <summary>Zincirin 1. adımını yürütebilmesi gereken işletme rolleri.</summary>
    public static TheoryData<string> BusinessRoles =>
    [
        MesnetRoles.CompanyManager,
        MesnetRoles.MasterTrainer,
        MesnetRoles.CompanyHR
    ];

    /// <summary>Sahibin saydığı okul onaycıları: "müdür yardımcısı ve müdür yeterli".</summary>
    public static TheoryData<string> SchoolApproverRoles =>
    [
        MesnetRoles.InstitutionManager,
        MesnetRoles.DeputyDirector
    ];

    private static IReadOnlyList<string> PermissionsOf(string role) =>
        RolePermissionMap.GetPermissionsForRoles([role]);

    [Theory]
    [MemberData(nameof(BusinessRoles))]
    public void Isletme_rolleri_birinci_adimi_yurutebilir(string role)
    {
        PermissionsOf(role).ShouldContain(Permissions.Attendance.LeaveBusinessApprove);
    }

    [Theory]
    [MemberData(nameof(SchoolApproverRoles))]
    public void Okul_onaycilari_ikinci_adimi_yurutebilir(string role)
    {
        PermissionsOf(role).ShouldContain(Permissions.Attendance.LeaveApprove);
    }

    /// <summary>
    /// Koordinatör öğretmen zincirde adım TUTMAZ — sahibin kararı: "müdür yardımcısı ve müdür
    /// yeterli ama öğretmene de izin bilgisini verelim, notifikasyon gibi düşün". Bildirim
    /// <c>PaidLeaveNotificationConsumer</c> ile gider; onay yetkisi verilmez.
    /// </summary>
    [Fact]
    public void Koordinator_ogretmen_ucretli_izni_onaylayamaz()
    {
        PermissionsOf(MesnetRoles.Teacher).ShouldNotContain(Permissions.Attendance.LeaveApprove);
    }

    /// <summary>Alan şefi ve kurum personeli de onaycı değildir — sahibin listesi iki roldür.</summary>
    [Theory]
    [InlineData(MesnetRoles.DepartmentHead)]
    [InlineData(MesnetRoles.InstitutionStaff)]
    public void Diger_okul_rolleri_ucretli_izni_onaylayamaz(string role)
    {
        PermissionsOf(role).ShouldNotContain(Permissions.Attendance.LeaveApprove);
    }

    /// <summary>Başvuru öğrencinindir; işletme rolleri öğrenci adına başvuru açamaz.</summary>
    [Fact]
    public void Basvuruyu_ogrenci_acar()
    {
        PermissionsOf(MesnetRoles.Student).ShouldContain(Permissions.Attendance.LeaveRequest);
    }

    [Theory]
    [MemberData(nameof(BusinessRoles))]
    public void Isletme_rolleri_ogrenci_adina_basvuru_acamaz(string role)
    {
        PermissionsOf(role).ShouldNotContain(Permissions.Attendance.LeaveRequest);
    }

    /// <summary>
    /// <b>Okul adımı bireysel ASLA atanamaz.</b> İşletme rollerinin atanabilir domain listesinde
    /// <c>attendance:</c> vardır (devamsızlık girişi ve rapor yükleme için gerekli). Bu kural
    /// olmasaydı müdür yardımcısı bir işletme kullanıcısına okul adımını atar, iki taraflı onay
    /// tek tarafa çökerdi. "Aynı kullanıcı iki adımı yapamaz" kuralı bunu KAPATMAZ — ikinci bir
    /// işletme kullanıcısı okul adımını yapardı.
    /// </summary>
    [Fact]
    public void Okul_adimi_bireysel_atanamaz()
    {
        AssignablePermissionScope.NeverDirectlyAssignable
            .ShouldContain(Permissions.Attendance.LeaveApprove);
    }

    [Theory]
    [MemberData(nameof(BusinessRoles))]
    public void Isletme_kullanicisina_okul_adimi_bireysel_atanamaz(string role)
    {
        AssignablePermissionScope.CanAssign(
                AssignablePermissionScope.Defaults, [role], Permissions.Attendance.LeaveApprove)
            .ShouldBeFalse();
    }

    /// <summary>Kurum müdürünün "*" kapsamı bile okul adımını bireysel atatamaz.</summary>
    [Fact]
    public void Kurum_muduru_yildiz_kapsamiyla_bile_okul_adimini_atayamaz()
    {
        AssignablePermissionScope.CanAssign(
                AssignablePermissionScope.Defaults,
                [MesnetRoles.InstitutionManager],
                Permissions.Attendance.LeaveApprove)
            .ShouldBeFalse();
    }

    /// <summary>
    /// <b>Wildcard gerçeği açıkça kilitlenir.</b> Okul müdürü işletme adımının iznine de sahiptir
    /// ve bu bir hata değildir — <c>attendance:*</c> öyle gerektirir, başka önek de kurtarmaz
    /// (müdürde <c>company:</c>, <c>department:</c>, <c>student:</c> wildcard'ları da var).
    /// Test bu gerçeği yazıya döker ki ileride biri "izinle çözelim" diye geri dönmesin: adımı
    /// koruyan şey <c>business_id</c> KAPSAMIdır ve müdürde o claim yoktur.
    /// </summary>
    [Fact]
    public void Mudur_isletme_adiminin_iznine_de_sahiptir_engel_kapsamdir()
    {
        PermissionsOf(MesnetRoles.InstitutionManager)
            .ShouldContain(Permissions.Attendance.LeaveBusinessApprove);
    }

    /// <summary>
    /// İşletme rolleri tek kişide birleşebilir (sahip = usta öğretici olabilir). Birleşim okul
    /// adımını DOĞURMAZ; yoksa tek kişi zincirin iki adımını da yürütürdü.
    /// </summary>
    [Theory]
    [InlineData(MesnetRoles.CompanyManager, MesnetRoles.MasterTrainer)]
    [InlineData(MesnetRoles.CompanyManager, MesnetRoles.CompanyHR)]
    [InlineData(MesnetRoles.MasterTrainer, MesnetRoles.CompanyHR)]
    public void Isletme_rolleri_birlestiginde_okul_adimi_dogmaz(string first, string second)
    {
        var permissions = RolePermissionMap.GetPermissionsForRoles([first, second]);

        permissions.ShouldContain(Permissions.Attendance.LeaveBusinessApprove);
        permissions.ShouldNotContain(Permissions.Attendance.LeaveApprove);
    }
}
