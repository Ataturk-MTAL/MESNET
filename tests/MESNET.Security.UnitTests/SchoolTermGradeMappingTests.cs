using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Okulda staj dönem notu giriş izninin rol haritasındaki yeri (#171).
///
/// <para>Bu izin, wildcard önek tuzağının (ADR-0001) <b>istenen sonucu verdiği</b> nadir
/// vakadır. Sahibin kararı notu alan şefi, müdür yardımcısı ve müdürün girebilmesi yönünde;
/// <c>department:*</c> wildcard'ı tam olarak bu üç roldedir. Diğer izinlerde bu önek
/// reddedilmişti (#126, #130, #172) çünkü oralarda hedef küme daha dardı — burada küme
/// birebir örtüşüyor.</para>
///
/// <para>Testler hem hedef kümeyi hem de dışarıda kalması gerekenleri kilitler: önek ileride
/// değiştirilirse ya alan şefi notu giremez ya da öğretmen/işletme girer hâle gelir.</para>
/// </summary>
public sealed class SchoolTermGradeMappingTests
{
    /// <summary>Sahibin saydığı küme: okulda staj notunu girebilenler.</summary>
    public static TheoryData<string> SchoolGradeRoles =>
    [
        MesnetRoles.InstitutionManager,
        MesnetRoles.DeputyDirector,
        MesnetRoles.DepartmentHead
    ];

    /// <summary>Girmemesi gereken taraflar.</summary>
    public static TheoryData<string> NonEntryRoles =>
    [
        MesnetRoles.Teacher,
        MesnetRoles.InstitutionStaff,
        MesnetRoles.CompanyManager,
        MesnetRoles.MasterTrainer,
        MesnetRoles.Student
    ];

    private static IReadOnlyList<string> PermissionsOf(string role) =>
        RolePermissionMap.GetPermissionsForRoles([role]);

    [Theory]
    [MemberData(nameof(SchoolGradeRoles))]
    public void Okul_yonetimi_ve_alan_sefi_okulda_staj_notunu_girebilir(string role)
    {
        PermissionsOf(role).ShouldContain(Permissions.DepartmentHead.SchoolGradeEnter);
    }

    [Theory]
    [MemberData(nameof(NonEntryRoles))]
    public void Diger_roller_okulda_staj_notunu_giremez(string role)
    {
        PermissionsOf(role).ShouldNotContain(Permissions.DepartmentHead.SchoolGradeEnter);
    }

    /// <summary>
    /// İşletme akışı DEĞİŞMEDİ (regresyon): işletmede staj notunu hâlâ işletme girer ve
    /// okulda staj izni ona geçmedi.
    /// </summary>
    [Fact]
    public void Isletme_notu_akisi_degismedi()
    {
        var permissions = PermissionsOf(MesnetRoles.CompanyManager);

        permissions.ShouldContain(Permissions.Company.EnterGrade);
        permissions.ShouldNotContain(Permissions.DepartmentHead.SchoolGradeEnter);
    }

    /// <summary>
    /// <b>Önek kararı kilitli.</b> İzin <c>department:</c> önekinde OLMALI: hedef küme
    /// <c>department:*</c> taşıyan üç rolle birebir aynı. <c>company:</c> önekinde olsaydı
    /// işletme rollerine geçerdi; <c>coordinator:</c> önekinde olsaydı koordinatör öğretmene
    /// geçerdi — ikisi de sahibin kararına aykırı.
    /// </summary>
    [Fact]
    public void Onek_department_olmali()
    {
        Permissions.DepartmentHead.SchoolGradeEnter.ShouldStartWith("department:");
        Permissions.DepartmentHead.SchoolGradeEnter.ShouldNotStartWith("company:");
        Permissions.DepartmentHead.SchoolGradeEnter.ShouldNotStartWith("coordinator:");
    }

    /// <summary>
    /// Alan şefi bu izni <c>department:*</c> wildcard'ıyla alır — açık satır olmasa bile.
    /// Test wildcard'ın gerçekten kapsadığını doğrular; kapsamasaydı açık satır tek dayanak
    /// olurdu ve haritadan silindiğinde sessizce kaybolurdu.
    /// </summary>
    [Fact]
    public void Alan_sefi_izni_wildcard_ile_de_alir()
    {
        RolePermissionMap
            .MatchesPermission("department:*", Permissions.DepartmentHead.SchoolGradeEnter)
            .ShouldBeTrue();
    }
}
