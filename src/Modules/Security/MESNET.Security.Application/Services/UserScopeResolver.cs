using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared.Security;

namespace MESNET.Security.Application.Services;

/// <summary>
/// Kullanıcı ve davet okumalarının kurum kapsamı — Security'nin <b>TEK</b> kapsam kapısı.
///
/// <para><b>Neden gerekli:</b> <c>UserAccount</c> ve <c>UserInvitation</c>
/// <c>DocumentTenancyMap</c>'te kimlik katmanındadır; conjoined kiracılık onları SÜZMEZ.
/// Kapsam kararının tamamı sorgu handler'ına aittir.</para>
///
/// <para><b>Kimlikler istekten HİÇ gelmez</b> — aktörün claim'lerinden türer. Kilitleyen test:
/// <c>IdentityDocumentScopeDriftTests</c>.</para>
/// </summary>
public sealed class UserScopeResolver
{
    private readonly ICurrentUserService _currentUser;
    private readonly IInstitutionSubtreeDirectory _subtree;

    public UserScopeResolver(ICurrentUserService currentUser, IInstitutionSubtreeDirectory subtree)
    {
        _currentUser = currentUser;
        _subtree = subtree;
    }

    /// <returns>
    /// <c>null</c> = süzgeç uygulanmaz (platform kapsamı). Aksi hâlde görünür kurum kimlikleri;
    /// boş liste geçerlidir ve "yalnız kurum bağı olmayan kayıtlar" demektir.
    /// </returns>
    public async Task<IReadOnlyList<Guid>?> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var scope = InstitutionScopePolicy.VisibleScope(
            _currentUser.GetCurrentUser()?.InstitutionId,
            _currentUser.GetInstitutionPath(),
            _currentUser.HasPermission(Permissions.Platform.TenantManage));

        // Alt ağaç sorgusu YALNIZ yol öneki varken yapılır — platform aktöründe gereksiz,
        // kimlik dalında anlamsız.
        var subtreeIds = string.IsNullOrWhiteSpace(scope.PathPrefix)
            ? []
            : await _subtree.GetSubtreeInstitutionIdsAsync(scope.PathPrefix, cancellationToken);

        return UserScopePolicy.VisibleInstitutionIds(scope, subtreeIds);
    }
}
