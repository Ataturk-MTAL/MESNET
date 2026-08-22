namespace MESNET.Common.Shared.Security;

/// <summary>
/// "Bu kullanıcıya alan (branş) kodu girilmesi zorunlu mu?" kararı (#126).
///
/// <para>Karar <b>rol adından değil permission'dan</b> türetilir: roller
/// <see cref="RolePermissionMap"/> ile permission kümesine çevrilir, sonra iki soru sorulur:</para>
///
/// <list type="number">
///   <item>Kullanıcı alan bazlı koordinasyon verisine yazabiliyor mu?
///         (<see cref="Permissions.DepartmentHead.Distribution"/>)</item>
///   <item>Kurum geneli muafiyeti var mı?
///         (<see cref="Permissions.Institution.AllBranches"/>)</item>
/// </list>
///
/// <para>Birincisi evet, ikincisi hayırsa → <b>en az bir alan zorunludur</b>; aksi hâlde
/// kullanıcı hiçbir alana yazamaz ve kaydedildiği anda kilitlenmiş olur.</para>
///
/// <para>Muafiyeti olan (okul müdürü, müdür yardımcısı) için alan <b>istenmez</b>: bu
/// kişiler hiçbir alana bağlı değildir ve boş liste onlarda doğru durumdur.</para>
/// </summary>
public static class BranchRequirement
{
    /// <summary>Verilen roller için alan kodu zorunlu mu?</summary>
    public static bool IsRequiredForRoles(IEnumerable<string>? roles)
    {
        if (roles is null)
            return false;

        var permissions = RolePermissionMap.GetPermissionsForRoles(roles);
        return IsRequiredForPermissions(permissions);
    }

    /// <summary>
    /// Verilen (genişletilmiş) permission kümesi için alan kodu zorunlu mu?
    /// Doğrudan (direct) atanmış yetkiler de hesaba katılmak istendiğinde bu aşırı yükleme kullanılır.
    /// </summary>
    public static bool IsRequiredForPermissions(IEnumerable<string>? permissions)
    {
        if (permissions is null)
            return false;

        var set = permissions.ToList();

        var hasExemption = set.Any(p =>
            RolePermissionMap.MatchesPermission(p, Permissions.Institution.AllBranches));

        if (hasExemption)
            return false;

        return set.Any(p =>
            RolePermissionMap.MatchesPermission(p, Permissions.DepartmentHead.Distribution));
    }
}
