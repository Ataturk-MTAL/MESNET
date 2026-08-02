using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// "Kendi verisi" kapsam merdiveni (#182).
///
/// <para><b>Kapatılan açık:</b> <c>attendance:view-own</c>, <c>internship:view-own</c> ve
/// <c>salary:view-own</c> tanımlıydı ve rollere dağıtılmıştı ama <b>hiçbir uçta
/// kullanılmıyordu</b>. Listeleme uçları okul tarafı iznini istiyordu; öğrenci kendi
/// devamsızlığını, veli de öğrencisininkini hiç göremiyordu. Kırık bir şey yoktu — akış
/// sessizce hiç başlamıyordu.</para>
///
/// <para>Merdivenin sırası kritiktir ve testler onu kilitler: geniş izin → veli bağı →
/// öğrencinin kendisi → <b>boş</b>. Son basamak "hepsini göster" olsaydı kapsamsız bir
/// kullanıcı tüm kurumun verisini görürdü.</para>
/// </summary>
public sealed class OwnDataScopeTests
{
    private static readonly Guid ChildA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ChildB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid SelfStudent = Guid.Parse("33333333-3333-3333-3333-333333333333");

    /// <summary>Okul tarafı: geniş izin var → sorgu daraltılmaz (bugünkü davranış korunur).</summary>
    [Fact]
    public void Genis_izin_varsa_kapsam_daraltilmaz()
    {
        var user = Fake(permissions: [Permissions.Attendance.View]);

        var scope = OwnDataScope.Resolve(user, Permissions.Attendance.View);

        scope.IsUnrestricted.ShouldBeTrue();
        scope.IsEmpty.ShouldBeFalse();
    }

    /// <summary>Wildcard da geniş izin sayılır — müdürde <c>attendance:*</c> var.</summary>
    [Fact]
    public void Wildcard_genis_izin_sayilir()
    {
        var user = Fake(permissions: RolePermissionMap.GetPermissionsForRoles([MesnetRoles.InstitutionManager]));

        OwnDataScope.Resolve(user, Permissions.Attendance.View).IsUnrestricted.ShouldBeTrue();
    }

    /// <summary>Veli: bağ kaydındaki öğrencilerle sınırlı.</summary>
    [Fact]
    public void Veli_yalniz_bagli_ogrencilerini_gorur()
    {
        var user = Fake(
            permissions: RolePermissionMap.GetPermissionsForRoles([MesnetRoles.Parent]),
            linkedStudentIds: [ChildA, ChildB]);

        var scope = OwnDataScope.Resolve(user, Permissions.Attendance.View);

        scope.IsUnrestricted.ShouldBeFalse();
        scope.StudentIds.ShouldBe([ChildA, ChildB]);
    }

    /// <summary>Öğrenci: yalnız kendisi.</summary>
    [Fact]
    public void Ogrenci_yalniz_kendisini_gorur()
    {
        var user = Fake(
            permissions: RolePermissionMap.GetPermissionsForRoles([MesnetRoles.Student]),
            studentId: SelfStudent);

        var scope = OwnDataScope.Resolve(user, Permissions.Attendance.View);

        scope.IsUnrestricted.ShouldBeFalse();
        scope.StudentIds.ShouldBe([SelfStudent]);
    }

    /// <summary>
    /// <b>Merdivenin sırası:</b> veli bağı, <c>student_id</c> claim'inden ÖNCE gelir. Bir
    /// kullanıcıda ikisi birden olursa (veli hesabı aynı zamanda öğrenci) bağ kazanır — daha
    /// geniş olan kapsam değil, açıkça kaydedilmiş olan seçilir.
    /// </summary>
    [Fact]
    public void Veli_bagi_kendi_ogrenci_kimliginden_once_gelir()
    {
        var user = Fake(
            permissions: RolePermissionMap.GetPermissionsForRoles([MesnetRoles.Parent]),
            linkedStudentIds: [ChildA],
            studentId: SelfStudent);

        OwnDataScope.Resolve(user, Permissions.Attendance.View).StudentIds.ShouldBe([ChildA]);
    }

    /// <summary>
    /// <b>Kapsamsız kullanıcı hiçbir şey görmez.</b> Son basamak "hepsini göster" olsaydı,
    /// <c>view-own</c> taşıyan ama bağı/kimliği çözülemeyen bir kullanıcı tüm kurumun verisini
    /// görürdü — bu iznin açtığı ucun tam tersi sonuç.
    /// </summary>
    [Fact]
    public void Kapsamsiz_kullanici_bos_sonuc_alir()
    {
        var user = Fake(permissions: [Permissions.Attendance.ViewOwn]);

        var scope = OwnDataScope.Resolve(user, Permissions.Attendance.View);

        scope.IsUnrestricted.ShouldBeFalse();
        scope.IsEmpty.ShouldBeTrue();
    }

    /// <summary>Boş <c>student_id</c> kapsam saymaz — eksik veri erişim doğurmamalı.</summary>
    [Fact]
    public void Bos_ogrenci_kimligi_kapsam_saymaz()
    {
        var user = Fake(permissions: [Permissions.Attendance.ViewOwn], studentId: Guid.Empty);

        OwnDataScope.Resolve(user, Permissions.Attendance.View).IsEmpty.ShouldBeTrue();
    }

    /// <summary>
    /// Üç modülün geniş izni ayrıdır: devamsızlık izni olan biri ücret listesinde
    /// otomatik geniş kapsam almaz.
    /// </summary>
    [Fact]
    public void Modul_izinleri_birbirinin_yerine_gecmez()
    {
        var user = Fake(permissions: [Permissions.Attendance.View]);

        OwnDataScope.Resolve(user, Permissions.Attendance.View).IsUnrestricted.ShouldBeTrue();
        OwnDataScope.Resolve(user, Permissions.Salary.View).IsUnrestricted.ShouldBeFalse();
    }

    // ── Birleşik policy adları ──

    /// <summary>
    /// Uçların istediği policy, iki izni de kabul etmelidir. Ad çözümlemesi bozulursa
    /// uç ya herkese kapanır ya da yanlış izne bağlanır.
    /// </summary>
    [Fact]
    public void Birlesik_policy_iki_izni_de_tasir()
    {
        PermissionPolicies.Split(PermissionPolicies.AttendanceViewOrOwn)
            .ShouldBe([Permissions.Attendance.View, Permissions.Attendance.ViewOwn]);

        PermissionPolicies.Split(PermissionPolicies.InternshipViewOrOwn)
            .ShouldBe([Permissions.Internship.View, Permissions.Internship.ViewOwn]);

        PermissionPolicies.Split(PermissionPolicies.SalaryViewOrOwn)
            .ShouldBe([Permissions.Salary.View, Permissions.Salary.ViewOwn]);
    }

    /// <summary>
    /// Birleşik policy adı bir izin sabitiyle ÇAKIŞMAMALI — çakışsaydı DI'da aynı adla iki
    /// policy kaydedilir ve hangisinin kazandığı belirsiz olurdu.
    /// </summary>
    [Fact]
    public void Birlesik_policy_adlari_izin_sabitleriyle_cakismaz()
    {
        var permissions = Permissions.GetAll().ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var policy in PermissionPolicies.All)
        {
            PermissionPolicies.IsCombined(policy).ShouldBeTrue($"{policy} birleşik olmalı.");
            permissions.ShouldNotContain(policy);
        }
    }

    /// <summary>Öğrenci ve velinin bu uçlara erişebilmesi için izinleri gerçekten olmalı.</summary>
    [Theory]
    [InlineData(MesnetRoles.Student)]
    [InlineData(MesnetRoles.Parent)]
    public void Veri_sahibi_rolleri_view_own_izinlerini_tasir(string role)
    {
        var permissions = RolePermissionMap.GetPermissionsForRoles([role]);

        permissions.ShouldContain(Permissions.Attendance.ViewOwn);
        permissions.ShouldContain(Permissions.Internship.ViewOwn);
        permissions.ShouldContain(Permissions.Salary.ViewOwn);

        // Geniş izinleri ALMAZ — uç açılır ama kapsam daralır.
        permissions.ShouldNotContain(Permissions.Attendance.View);
        permissions.ShouldNotContain(Permissions.Internship.View);
        permissions.ShouldNotContain(Permissions.Salary.View);
    }

    private static ICurrentUserService Fake(
        IReadOnlyList<string> permissions,
        IReadOnlyList<Guid>? linkedStudentIds = null,
        Guid? studentId = null) =>
        new FakeCurrentUser(permissions, linkedStudentIds ?? [], studentId);

    /// <summary>Rol adı taşımayan sahte kullanıcı — kapsam kararı yalnız izin + claim ile verilir.</summary>
    private sealed class FakeCurrentUser(
        IReadOnlyList<string> permissions,
        IReadOnlyList<Guid> linkedStudentIds,
        Guid? studentId) : ICurrentUserService
    {
        public UserContext? GetCurrentUser() =>
            new(Guid.NewGuid(), "Test Kullanıcı",
                StudentId: studentId, Permissions: permissions, LinkedStudentIds: linkedStudentIds);

        public Guid GetUserId() => Guid.Empty;

        public string GetFullName() => "Test Kullanıcı";

        public bool HasPermission(string permission) =>
            permissions.Any(p => RolePermissionMap.MatchesPermission(p, permission));

        public bool IsInRole(string role) => false;

        public IReadOnlyList<string> GetBranchCodes() => [];

        public IReadOnlyList<Guid> GetLinkedStudentIds() => linkedStudentIds;
    }
}
