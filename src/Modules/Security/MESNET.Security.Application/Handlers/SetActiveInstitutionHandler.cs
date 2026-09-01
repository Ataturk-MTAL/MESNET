using Marten;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Infrastructure.Tenancy;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Security;
using MESNET.Security.Application.Commands;
using MESNET.Security.Application.Errors;
using MESNET.Security.Core.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace MESNET.Security.Application.Handlers;

/// <summary>
/// Aktif bağlamı değiştirir.
/// </summary>
/// <remarks>
/// <para><b>Ayrı bir izin GEREKTİRMEZ</b> ve bu bilinçlidir: kapı iznin kendisi değil, alt
/// ağaç kontrolüdür. Okul kullanıcısının alt ağacı yalnız kendisidir; onun için bağlam
/// değiştirmek işlevsizdir, yasak değil. Ayrı bir izin, kapının ikinci bir kopyasını
/// üretmekten başka bir şey yapmazdı.</para>
///
/// <para><b>Önbellek geçersizleme atlanamaz.</b> <c>PermissionClaimsTransformation</c>
/// kullanıcı claim'lerini beş dakika önbellekliyor; çağrılmazsa yeni bağlam o süre boyunca
/// görünmez ve kullanıcı hâlâ eski okulda çalıştığını sanır.</para>
///
/// <para><b>UserAccount, KeycloakUserId (token'ın <c>sub</c>'ı) ile bulunur, Id ile DEĞİL.</b>
/// <c>UserAccount.Id</c> yerelde <c>Guid.NewGuid()</c> ile üretilir ve Keycloak kimliğiyle
/// eşleşmez; <c>ICurrentUserService.GetCurrentUser().UserId</c> ise token'ın <c>sub</c>'ından
/// ayrıştırılmış Guid'dir. Aynı ayrım <c>InvitationHandler.ResolveActorNamesAsync</c> ve
/// <c>StudentAccountSyncConsumer</c>'da da var.</para>
/// </remarks>
public static class SetActiveInstitutionHandler
{
    public static async Task Handle(
        SetActiveInstitution command,
        ICurrentUserService currentUser,
        IDocumentSession session,
        IInstitutionPathLookup pathLookup,
        IMemoryCache cache,
        CancellationToken cancellationToken)
    {
        var actor = currentUser.GetCurrentUser()
            ?? throw new DomainException(SecurityErrors.ActiveContextOutOfScope(
                command.InstitutionId ?? Guid.Empty));

        var keycloakUserId = actor.UserId.ToString();

        var account = await session.Query<UserAccount>()
            .FirstOrDefaultAsync(a => a.KeycloakUserId == keycloakUserId && a.DeletedAt == null, cancellationToken)
            ?? throw new DomainException(SecurityErrors.UserNotFound(actor.UserId));

        if (command.InstitutionId is { } target && target != Guid.Empty)
        {
            var hasPlatformScope = currentUser.HasPermission(Permissions.Platform.TenantManage);
            var targetPath = await pathLookup.GetPathAsync(target, cancellationToken);

            if (!CanSwitchTo(actor.InstitutionId, actor.InstitutionPath, target, targetPath, hasPlatformScope))
            {
                throw new DomainException(SecurityErrors.ActiveContextOutOfScope(target));
            }

            account.ActiveInstitutionId = target;
            account.ActiveContextSessionId = currentUser.GetSessionId();
        }
        else
        {
            account.ActiveInstitutionId = null;
            account.ActiveContextSessionId = null;
        }

        account.UpdatedAt = DateTime.UtcNow;
        session.Store(account);
        await session.SaveChangesAsync(cancellationToken);

        PermissionClaimsTransformation.InvalidateCache(cache, account.KeycloakUserId);
    }

    /// <summary>
    /// Bağlam geçişinin kapsam kararı — saf yardımcı, Marten/HTTP'den bağımsız. Kural burada
    /// YAŞAMAZ; <see cref="InstitutionScopePolicy.Decide"/> ve
    /// <see cref="InstitutionScopePolicy.CanAccessByPath"/>'i sarar, tek iş ikisini bu ucun
    /// beklediği tek boole karara indirgemektir.
    /// </summary>
    /// <remarks>
    /// <para><b>Platform muafiyeti burada eksikti — canlıda ölçüldü.</b> Bu uç eskiden yalnız
    /// "kendi kurumu" veya "aktörün alt ağacı"nı kabul ediyordu; <c>Decide</c>'ın zaten taşıdığı
    /// <c>hasPlatformScope</c> parametresi hiç geçilmiyordu. <c>platform:tenant:manage</c>
    /// taşıyan aktör zaten bütün kurumları okuyabiliyor
    /// (<see cref="InstitutionScopePolicy.VisibleScope"/> → <c>Unrestricted</c>) ve kullanıcıyı
    /// herhangi bir okula bağlayabiliyor (ADR-0003 adım 6,
    /// <see cref="UserInstitutionScopePolicy.CanAssign"/>). Bağlam değiştirmesini engellemek
    /// yeni bir güvence üretmiyordu — yalnız çözümleme katmanının
    /// (<c>TenantResolution.Resolve</c>, <c>TenantResolutionActiveContextTests.
    /// Kurumu_olmayan_platform_aktoru_baglam_secebilir</c>) zaten desteklediği bir durumu bu
    /// ucun veremediği bir tutarsızlık bırakıyordu. Her geçiş zaten denetim izine düşer
    /// (<c>AuditCommandLabels</c>) — muafiyet yeni bir izlenemez yol açmaz.</para>
    /// </remarks>
    public static bool CanSwitchTo(
        Guid? actorInstitutionId,
        string? actorPath,
        Guid targetInstitutionId,
        string? targetPath,
        bool hasPlatformScope)
    {
        var outcome = InstitutionScopePolicy.Decide(actorInstitutionId, targetInstitutionId, hasPlatformScope);

        return outcome switch
        {
            InstitutionScopeOutcome.Allowed => true,
            InstitutionScopeOutcome.NeedsPathCheck => InstitutionScopePolicy.CanAccessByPath(actorPath, targetPath),
            _ => false,
        };
    }
}
