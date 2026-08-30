using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Institution.Application.Errors;

namespace MESNET.Institution.Application.Security;

/// <summary>
/// Kurum kapsamı guard'ı (ADR-0003 adım 6 + kurum hiyerarşisi).
/// <see cref="IInstitutionScoped"/> taşıyan her command/query'den önce çalışır: aktörün
/// kapsamı hedefi içermiyorsa <see cref="DomainException"/> fırlatır (HTTP 422).
///
/// <para><b>Karar burada değil, saf <see cref="InstitutionScopePolicy"/> içindedir</b>; burası
/// yalnız girdileri toplar ve gerekiyorsa hedefin yolunu okur. Aynı ayrım
/// <c>BranchScopeGuard</c>'da da var.</para>
///
/// <para><b>Sıcak yolda ek okuma YOKTUR.</b> Okul kullanıcısının kendi kurumuna erişiminde
/// aktör ve hedef kimlikleri eşittir; karar <see cref="InstitutionScopePolicy.Decide"/>
/// içinde biter ve veritabanına hiç gidilmez. Hedefin yolu yalnız kimlikler ayrıştığında —
/// yani yeni il/ilçe yeteneği kullanıldığında — okunur.</para>
///
/// <para><b>Okumada da çalışır</b> — alan kapsamının aksine. Alan şefinin başka alanın
/// dağıtımını görmesi bilinçli olarak açıktı; başka <i>okulun</i> kaydını görmek değildir.
/// Ölçüldü: kontrol yokken bir okul müdürü diğer okulun <b>personel listesini</b> okuyordu.</para>
/// </summary>
public static class InstitutionScopeGuardMiddleware
{
    public static async Task BeforeAsync(
        IInstitutionScoped message, ICurrentUserService currentUser, IQuerySession session)
    {
        var actor = currentUser.GetCurrentUser();
        var hasPlatformScope = currentUser.HasPermission(Permissions.Platform.TenantManage);

        var outcome = InstitutionScopePolicy.Decide(
            actor?.InstitutionId, message.InstitutionId, hasPlatformScope);

        if (outcome == InstitutionScopeOutcome.Allowed)
            return;

        if (outcome == InstitutionScopeOutcome.NeedsPathCheck)
        {
            var target = await session
                .LoadAsync<Core.Entities.Institution>(message.InstitutionId);

            // Var olmayan hedef reddedilir, "bulunamadı" DENMEZ: kapsamı olmayan bir aktöre
            // hangi kimliklerin var olduğunu doğrulatmak, kurum listesini tahminle taramanın
            // kapısını açar. Aynı gerekçe InstitutionErrors.InstitutionScopeDenied yorumunda.
            if (target is not null
                && InstitutionScopePolicy.CanAccessByPath(actor?.InstitutionPath, target.Path))
            {
                return;
            }
        }

        throw new DomainException(InstitutionErrors.InstitutionScopeDenied(message.InstitutionId));
    }
}
