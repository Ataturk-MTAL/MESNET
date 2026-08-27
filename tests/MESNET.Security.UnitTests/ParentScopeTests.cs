using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using Shouldly;
using Xunit;

namespace MESNET.Security.UnitTests;

/// <summary>
/// Veli aktörünün izin demeti ve kapsam kuralları (#174).
///
/// <para><b>Velinin kapsamı permission ile verilemez</b> (ADR-0001). Tüm velilerin izinleri
/// aynıdır; onları birbirinden ayıran tek şey hangi öğrenciye bağlı olduklarıdır ve bu bilgi
/// bir izin değil bir <b>kayıt</b>tır (<c>UserAccount.LinkedStudentIds</c>). İzin bazlı bir
/// çözüm ya her veliyi her öğrenciye açardı ya da öğrenci başına izin üretmeyi gerektirirdi.</para>
/// </summary>
public sealed class ParentScopeTests
{
    private static readonly Guid OwnChild = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherChild = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static IReadOnlyList<string> ParentPermissions =>
        RolePermissionMap.GetPermissionsForRoles([MesnetRoles.Parent]);

    // ── Kapsam politikası ──

    [Fact]
    public void Veli_bagli_oldugu_ogrenciye_erisir()
    {
        ParentScopePolicy.CanAccessStudent([OwnChild], OwnChild).ShouldBeTrue();
    }

    [Fact]
    public void Veli_baska_ogrenciye_erisemez()
    {
        ParentScopePolicy.CanAccessStudent([OwnChild], OtherChild).ShouldBeFalse();
    }

    /// <summary>
    /// <b>Boş liste erişim vermez.</b> <c>BranchCodes</c>'ta boş liste "alana bağlı değil"
    /// demekti ve muafiyeti olan roller için normaldi; burada boş liste <b>bağ kurulmamış</b>
    /// demektir. Tersi yorumlansaydı bağı silinen veli her öğrenciye erişirdi.
    /// </summary>
    [Fact]
    public void Bos_bag_listesi_hicbir_ogrenciye_erisim_vermez()
    {
        ParentScopePolicy.CanAccessStudent([], OwnChild).ShouldBeFalse();
        ParentScopePolicy.CanAccessStudent(null, OwnChild).ShouldBeFalse();
        ParentScopePolicy.HasLinkedStudents([]).ShouldBeFalse();
        ParentScopePolicy.HasLinkedStudents(null).ShouldBeFalse();
    }

    /// <summary>Boş kimlik hiçbir koşulda eşleşmemeli — aksi hâlde eksik veri kapsamı açardı.</summary>
    [Fact]
    public void Bos_ogrenci_kimligi_eslesme_saymaz()
    {
        ParentScopePolicy.CanAccessStudent([Guid.Empty], Guid.Empty).ShouldBeFalse();
    }

    [Fact]
    public void Normalize_bos_ve_yinelenen_kimlikleri_atar()
    {
        ParentScopePolicy.Normalize([OwnChild, OwnChild, Guid.Empty, OtherChild])
            .ShouldBe([OwnChild, OtherChild]);

        ParentScopePolicy.Normalize(null).ShouldBeEmpty();
    }

    // ── İzin demeti ──

    /// <summary>
    /// Veli veri GİRER: sağlık raporu ve ücretli izin başvurusu. Bunlar #172 ve #177'nin
    /// "giriş geniş" tarafıdır.
    /// </summary>
    [Theory]
    [InlineData("attendance:upload")]
    [InlineData("attendance:leave:request")]
    public void Veli_giris_yapabilir(string permission)
    {
        ParentPermissions.ShouldContain(permission);
    }

    /// <summary>
    /// <b>Hiçbir girişi hüküm doğurmaz.</b> Veli, öğrencisinin ücret kesintisini tek taraflı
    /// kaldıramaz — yüklediği rapor koordinatör öğretmen onayına düşer (#172).
    /// </summary>
    [Theory]
    [InlineData("attendance:direct-entry")]
    [InlineData("attendance:health-report:direct")]
    [InlineData("attendance:approve")]
    [InlineData("attendance:manage")]
    public void Veli_hukum_dogurmaz_ve_onaylamaz(string permission)
    {
        ParentPermissions.ShouldNotContain(permission);
    }

    /// <summary>
    /// Veli fesih zincirinde <b>onaycı değildir</b> (#218).
    ///
    /// <para>Model düzeltildi: veli ve işletme yetkilisi fesih <b>talep eder</b>, onaylamaz.
    /// Zincir koordinatör öğretmen → müdür yardımcısı → müdürden ibarettir. Bu yüzden
    /// <c>internship:approve:parent</c> izni de kaldırıldı — hiçbir uca bağlı değildi ve
    /// olmayan bir yetkiyi varmış gibi gösteriyordu.</para>
    /// </summary>
    [Fact]
    public void Veli_fesih_zincirinde_onaycu_degildir()
    {
        ParentPermissions.ShouldNotContain(Permissions.Internship.Approve);
        ParentPermissions.ShouldNotContain(Permissions.Internship.Manage);
        ParentPermissions.ShouldNotContain("internship:approve:parent");
    }

    /// <summary>Veli okul/işletme tarafının hiçbir yönetim iznini almaz.</summary>
    [Theory]
    [InlineData("student:view")]
    [InlineData("student:manage")]
    [InlineData("attendance:view")]
    [InlineData("company:view")]
    [InlineData("user:view")]
    [InlineData("institution:view")]
    [InlineData("department:distribution:manage")]
    public void Veli_yonetim_izni_almaz(string permission)
    {
        ParentPermissions.ShouldNotContain(permission);
    }

    // ── Guard davranışı ──

    /// <summary>
    /// <b>Bağı olmayan kullanıcı bu guard'dan etkilenmez.</b> Okul ve işletme tarafının kapsamı
    /// kurum/işletme claim'lerinden gelir; guard yalnız bağ kapsamını uygular, erişim kararını
    /// değil. Aksi kurgulansaydı guard eklendiği her uçta okul tarafını da kilitlerdi.
    /// </summary>
    [Fact]
    public void Bagi_olmayan_kullanici_guarddan_etkilenmez()
    {
        var schoolUser = new FakeCurrentUser([]);

        ParentScopeGuard.CanAccessStudent(schoolUser, OwnChild).ShouldBeTrue();
        Should.NotThrow(() => ParentScopeGuard.EnsureCanAccessStudent(schoolUser, OtherChild));
    }

    [Fact]
    public void Bagli_veli_kendi_ogrencisine_erisir()
    {
        var parent = new FakeCurrentUser([OwnChild]);

        ParentScopeGuard.CanAccessStudent(parent, OwnChild).ShouldBeTrue();
        Should.NotThrow(() => ParentScopeGuard.EnsureCanAccessStudent(parent, OwnChild));
    }

    /// <summary>Başka öğrenciye dokunma denemesi 422 üretir — sessizce boş sonuç DEĞİL.</summary>
    [Fact]
    public void Bagli_veli_baska_ogrenciye_dokunamaz()
    {
        var parent = new FakeCurrentUser([OwnChild]);

        ParentScopeGuard.CanAccessStudent(parent, OtherChild).ShouldBeFalse();

        var ex = Should.Throw<DomainException>(
            () => ParentScopeGuard.EnsureCanAccessStudent(parent, OtherChild));
        ex.Error.Code.ShouldBe("PARENT_SCOPE_VIOLATION");
    }

    /// <summary>Rol adı taşımayan sahte kullanıcı — kapsam kararı yalnız bağ kaydından verilir.</summary>
    private sealed class FakeCurrentUser(IReadOnlyList<Guid> linkedStudentIds) : ICurrentUserService
    {
        public UserContext? GetCurrentUser() =>
            new(Guid.NewGuid(), "Test Kullanıcı", LinkedStudentIds: linkedStudentIds);

        public Guid GetUserId() => Guid.Empty;

        public string GetFullName() => "Test Kullanıcı";

        public bool HasPermission(string permission) => false;

        public bool IsInRole(string role) => false;

        public IReadOnlyList<string> GetBranchCodes() => [];

        public IReadOnlyList<Guid> GetLinkedStudentIds() => linkedStudentIds;

        public string? GetInstitutionPath() => null;
    }

    /// <summary>
    /// Veliye <c>attendance:</c> ve <c>internship:</c> domainlerinden <b>bireysel izin
    /// atanamaz</b>. Atanabilseydi, kapsamı bağ kaydıyla sınırlı bir kullanıcıya okul tarafının
    /// uçları açılabilirdi.
    /// </summary>
    [Theory]
    [InlineData("attendance:direct-entry")]
    [InlineData("attendance:approve")]
    [InlineData("internship:approve")]
    public void Veliye_okul_izni_bireysel_atanamaz(string permission)
    {
        AssignablePermissionScope.CanAssign(
                AssignablePermissionScope.Defaults, [MesnetRoles.Parent], permission)
            .ShouldBeFalse();
    }
}
