using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Coordination.Application.Errors;

namespace MESNET.Coordination.Application.Security;

/// <summary>
/// Koordinasyon <b>yazma</b> handler'larında alan (branş) kapsamı kontrolü (#126).
///
/// <para>Kararın kendisi saf <see cref="BranchScopePolicy"/> içindedir; burası yalnız
/// <see cref="ICurrentUserService"/>'ten girdileri toplayıp ihlalde
/// <see cref="DomainException"/> fırlatır (HTTP 422).</para>
///
/// <para><b>Okuma uçlarında kullanılmaz.</b> Alan şefinin başka alanın dağıtımını
/// görmesi bilinçli olarak açıktır — koordinasyon bütününü görmek işe yarar; kapatılan
/// yalnız değiştirmedir.</para>
/// </summary>
public static class BranchScopeGuard
{
    /// <summary>
    /// Kullanıcı <paramref name="branchCode"/> alanına yazamıyorsa <see cref="DomainException"/> fırlatır.
    /// </summary>
    /// <param name="branchCode">
    /// Hedef alan kodu. Mümkün olduğunda <b>çözümlenmiş satırın</b> alan kodu verilmelidir —
    /// istek parametresi boş bırakılarak kontrol atlatılamasın.
    /// </param>
    public static void EnsureCanWrite(ICurrentUserService currentUser, string? branchCode)
    {
        var userBranchCodes = currentUser.GetBranchCodes();
        var hasAllBranches = currentUser.HasPermission(Permissions.Institution.AllBranches);

        if (BranchScopePolicy.CanWrite(branchCode, userBranchCodes, hasAllBranches))
            return;

        throw new DomainException(
            CoordinationErrors.BranchScopeDenied(branchCode ?? string.Empty, userBranchCodes));
    }
}
