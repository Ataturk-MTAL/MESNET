using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Okulda staj dönem notu giriş izninin rol haritasındaki yeri (#171).
///
/// <para><b>Önek <c>institution:</c>:</b> öğrenci okulda staj yaptığında kurum, işverenin
/// yerine geçer — bu bir alan/bölüm işi değil, kurumun işidir. Sahibin kararı: <i>"Resmî
/// kuruma bağlı izinler kurumsal olmalı."</i></para>
///
/// <para><b>Bu önekte wildcard hedefi TEK BAŞINA karşılamaz:</b> <c>institution:*</c> yalnız
/// <c>InstitutionManager</c>'dadır. Müdür yardımcısı ve alan şefi izni yalnız
/// <see cref="RolePermissionMap"/>'teki <b>açık satırlarla</b> alır — satır silinirse izni
/// sessizce kaybederler. Aşağıdaki testler o satırların varlığını kilitler.</para>
///
/// <para><b>Önek kapsamı belirlemez</b> (ADR-0001): "hangi kurumun öğrencisi" sorusunu
/// <c>institution_id</c> claim'i cevaplar ve o kontrol izinden bağımsız çalışır.</para>
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
        PermissionsOf(role).ShouldContain(Permissions.Institution.SchoolGradeEnter);
    }

    [Theory]
    [MemberData(nameof(NonEntryRoles))]
    public void Diger_roller_okulda_staj_notunu_giremez(string role)
    {
        PermissionsOf(role).ShouldNotContain(Permissions.Institution.SchoolGradeEnter);
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
        permissions.ShouldNotContain(Permissions.Institution.SchoolGradeEnter);
    }

    /// <summary>
    /// <b>Önek kararı kilitli.</b> İzin <c>institution:</c> önekinde OLMALI — okulda staj
    /// kurumun işidir. <c>company:</c> önekinde olsaydı işletme rollerine geçerdi;
    /// <c>coordinator:</c> önekinde olsaydı koordinatör öğretmene geçerdi.
    /// </summary>
    [Fact]
    public void Onek_institution_olmali()
    {
        Permissions.Institution.SchoolGradeEnter.ShouldStartWith("institution:");
        Permissions.Institution.SchoolGradeEnter.ShouldNotStartWith("company:");
        Permissions.Institution.SchoolGradeEnter.ShouldNotStartWith("coordinator:");
    }

    /// <summary>
    /// <b>Wildcard bu izni tek başına dağıtmaz.</b> <c>institution:*</c> yalnız müdürdedir;
    /// müdür yardımcısı ve alan şefi izni açık satırla alır. Test bu gerçeği yazıya döker:
    /// "zaten wildcard kapsıyor" diye açık satırları silen biri o iki rolü sessizce yetkisiz
    /// bırakır — belirti ancak dönem sonunda, not girilemediğinde çıkar.
    /// </summary>
    [Fact]
    public void Wildcard_tek_basina_yetmez_acik_satirlar_sarttir()
    {
        // Müdür wildcard'la alır.
        RolePermissionMap
            .MatchesPermission("institution:*", Permissions.Institution.SchoolGradeEnter)
            .ShouldBeTrue();

        // Diğer iki rolün TAŞIDIĞI wildcard'lar bu izni kapsamaz — dayanak açık satırdır.
        RolePermissionMap
            .MatchesPermission("department:*", Permissions.Institution.SchoolGradeEnter)
            .ShouldBeFalse();
    }

    /// <summary>
    /// Alan şefinin <c>institution:</c> önekli BAŞKA hiçbir izni olmamalı. Bu izin ona açık
    /// satırla verildi; ileride <c>institution:*</c> ya da kurum yönetimi izinleri (silme,
    /// personel yetkilendirme) eklenirse alan şefi sessizce kurum yöneticisi olurdu.
    /// </summary>
    [Fact]
    public void Alan_sefinin_baska_kurum_izni_yoktur()
    {
        var institutionPermissions = PermissionsOf(MesnetRoles.DepartmentHead)
            .Where(p => p.StartsWith("institution:", StringComparison.Ordinal))
            .ToList();

        institutionPermissions.ShouldBe([Permissions.Institution.SchoolGradeEnter]);
    }
}
