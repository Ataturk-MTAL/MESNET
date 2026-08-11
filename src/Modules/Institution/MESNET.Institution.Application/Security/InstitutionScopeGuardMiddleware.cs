using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Institution.Application.Errors;

namespace MESNET.Institution.Application.Security;

/// <summary>
/// Kurum kapsamı guard'ı (ADR-0003 adım 6). <see cref="IInstitutionScoped"/> taşıyan her
/// command/query'den önce çalışır: aktörün kurum claim'i hedefle eşleşmiyorsa
/// <see cref="DomainException"/> fırlatır (HTTP 422).
///
/// <para><b>Karar burada değil, saf <see cref="InstitutionScopePolicy"/> içindedir</b>; burası
/// yalnız <see cref="ICurrentUserService"/>'ten girdileri toplar. Aynı ayrım
/// <c>BranchScopeGuard</c>'da da var.</para>
///
/// <para><b>Okumada da çalışır</b> — alan kapsamının aksine. Alan şefinin başka alanın
/// dağıtımını görmesi bilinçli olarak açıktı; başka <i>okulun</i> kaydını görmek değildir.
/// Ölçüldü: kontrol yokken bir okul müdürü diğer okulun <b>personel listesini</b> okuyordu.</para>
/// </summary>
public static class InstitutionScopeGuardMiddleware
{
    public static void Before(IInstitutionScoped message, ICurrentUserService currentUser)
    {
        var actorInstitutionId = currentUser.GetCurrentUser()?.InstitutionId;
        var hasPlatformScope = currentUser.HasPermission(Permissions.Platform.TenantManage);

        if (InstitutionScopePolicy.CanAccess(actorInstitutionId, message.InstitutionId, hasPlatformScope))
            return;

        throw new DomainException(InstitutionErrors.InstitutionScopeDenied(message.InstitutionId));
    }
}
