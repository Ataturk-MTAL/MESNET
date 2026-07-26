using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Coordination.Application.Security;
using Shouldly;
using Xunit;

namespace MESNET.Coordination.UnitTests;

/// <summary>
/// Yazma handler'larının kullandığı kapsam kapısı (#126): ihlalde
/// <see cref="DomainException"/> → HTTP 422 (projede yerleşik desen).
/// </summary>
public sealed class BranchScopeGuardTests
{
    private const string Eet = "EET";
    private const string Mtt = "MTT";

    /// <summary>Rol adı taşımayan sahte kullanıcı — kapsam kararı yalnız claim + permission ile verilir.</summary>
    private sealed class FakeCurrentUser(
        IReadOnlyList<string> branchCodes,
        IReadOnlyList<string> permissions) : ICurrentUserService
    {
        public UserContext? GetCurrentUser() =>
            new(Guid.NewGuid(), "Test Kullanıcı", BranchCodes: branchCodes, Permissions: permissions);

        public Guid GetUserId() => Guid.Empty;

        public string GetFullName() => "Test Kullanıcı";

        public bool HasPermission(string permission) =>
            permissions.Any(p => RolePermissionMap.MatchesPermission(p, permission));

        public bool IsInRole(string role) => false;

        public IReadOnlyList<string> GetBranchCodes() => branchCodes;
    }

    private static ICurrentUserService DepartmentHead(params string[] branchCodes) =>
        new FakeCurrentUser(branchCodes, RolePermissionMap.GetPermissionsForRoles([MesnetRoles.DepartmentHead]));

    private static ICurrentUserService WithRole(string role, params string[] branchCodes) =>
        new FakeCurrentUser(branchCodes, RolePermissionMap.GetPermissionsForRoles([role]));

    [Fact]
    public void Alan_sefi_kendi_alanina_yazabilir()
    {
        Should.NotThrow(() => BranchScopeGuard.EnsureCanWrite(DepartmentHead(Eet), Eet));
    }

    [Fact]
    public void Alan_sefi_baska_alana_yazamaz()
    {
        var ex = Should.Throw<DomainException>(
            () => BranchScopeGuard.EnsureCanWrite(DepartmentHead(Eet), Mtt));

        ex.Error.Code.ShouldBe("Coordination.BranchScopeDenied");
    }

    [Fact]
    public void Alan_bilgisi_olmayan_alan_sefi_hicbir_alana_yazamaz()
    {
        Should.Throw<DomainException>(
            () => BranchScopeGuard.EnsureCanWrite(DepartmentHead(), Eet));
    }

    [Fact]
    public void Birden_cok_alandan_sorumlu_alan_sefi_her_ikisine_de_yazabilir()
    {
        var user = DepartmentHead(Eet, Mtt);

        Should.NotThrow(() => BranchScopeGuard.EnsureCanWrite(user, Eet));
        Should.NotThrow(() => BranchScopeGuard.EnsureCanWrite(user, Mtt));
    }

    /// <summary>
    /// Okul müdürü ve müdür yardımcısı hiçbir alana bağlı değildir — <c>branch_codes</c>
    /// tamamen boştur ve bu doğru durumdur. Muafiyet izni alan listesinden bağımsız
    /// çalışmalıdır; aksi hâlde yöneticiler koordinasyona yazamaz hâle gelirdi.
    /// </summary>
    [Theory]
    [InlineData(MesnetRoles.InstitutionManager)]
    [InlineData(MesnetRoles.InstitutionStaff)]
    public void Muafiyetli_yonetici_branch_codes_TAMAMEN_BOSKEN_her_alana_yazabilir(string role)
    {
        var user = WithRole(role); // hiç alan kodu yok — beklenen normal durum

        user.GetBranchCodes().ShouldBeEmpty();
        Should.NotThrow(() => BranchScopeGuard.EnsureCanWrite(user, Eet));
        Should.NotThrow(() => BranchScopeGuard.EnsureCanWrite(user, Mtt));
    }
}
