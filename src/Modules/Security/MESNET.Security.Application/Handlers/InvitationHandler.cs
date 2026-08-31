using Marten;
using MESNET.Common.Infrastructure.Email;
using MESNET.Common.Infrastructure.Pagination;
using MESNET.Common.Infrastructure.Security;
using MESNET.Common.Shared;
using MESNET.Common.Shared.Pagination;
using MESNET.Common.Shared.Security;
using MESNET.Security.Application.Commands;
using MESNET.Security.Application.Errors;
using MESNET.Security.Application.Events;
using MESNET.Security.Application.Services;
using MESNET.Security.Core.Entities;
using MESNET.Security.Core.Enums;
using MESNET.Security.Shared.Events;

namespace MESNET.Security.Application.Handlers;

public static class CreateInvitationHandler
{
    public static async Task<(Guid, InvitationCreated)> Handle(
        CreateInvitation command, IDocumentSession session, ICurrentUserService currentUser)
    {
        var studentIds = ParentScopePolicy.Normalize(command.StudentIds ?? []).ToList();

        // KAPSAM İSTEKTEN ALINMAZ (#271). Öğrenci kimlikleri istek gövdesinden geliyor; kontrol
        // olmadan bir okulun yöneticisi başka okulun öğrencisini kendi kullanıcısına
        // bağlayabilir ve ParentScopeGuard o listeye sorgusuz güvenir.
        await GuardianLinkScopeGuard.EnsureInScopeAsync(session, studentIds);

        var existing = await session.Query<UserInvitation>()
            .Where(i => i.Email == command.Email
                        && i.TargetRole == command.TargetRole
                        && (i.StatusName == InvitationStatus.PendingApproval.Name || i.StatusName == InvitationStatus.Approved.Name))
            .FirstOrDefaultAsync();

        if (existing is not null)
            throw new DomainException(SecurityErrors.InvitationAlreadyExists(command.Email, command.TargetRole));

        var invitation = new UserInvitation
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            TargetRole = command.TargetRole,
            InstitutionId = command.InstitutionId,
            BusinessId = command.BusinessId,
            // Veli–öğrenci bağı (#271) — kabul anında UserAccount'a yazılır.
            StudentIds = studentIds,
            // Aktör token'dan gelir, istekten DEĞİL (#137).
            CreatedById = currentUser.GetUserId(),
            Metadata = command.Metadata ?? [],
            Status = InvitationStatus.PendingApproval
        };

        session.Store(invitation);

        var @event = new InvitationCreated(
            invitation.Id, command.Email, invitation.FullName,
            command.TargetRole, command.InstitutionId, command.BusinessId);

        return (invitation.Id, @event);
    }
}

public static class ApproveInvitationHandler
{
    public static async Task<InvitationApproved> Handle(
        ApproveInvitation command, IDocumentSession session, IEmailService emailService,
        ICurrentUserService currentUser)
    {
        var invitation = await session.LoadAsync<UserInvitation>(command.InvitationId);
        if (invitation is null)
            throw new DomainException(SecurityErrors.InvitationNotFound(command.InvitationId));

        if (invitation.Status != InvitationStatus.PendingApproval)
            throw new DomainException(SecurityErrors.InvalidInvitationStatus(
                command.InvitationId, invitation.Status.Slug, InvitationStatus.PendingApproval.Slug));

        // Aktör token'dan gelir, istekten DEĞİL (#137).
        var approvedById = currentUser.GetUserId();

        invitation.Status = InvitationStatus.Approved;
        invitation.ApprovedAt = DateTime.UtcNow;
        invitation.ApprovedById = approvedById;
        invitation.ExpiresAt = DateTime.UtcNow.AddDays(7);
        session.Store(invitation);

        await emailService.SendInvitationEmailAsync(
            invitation.Email, invitation.FullName, invitation.TargetRole, invitation.Id);

        return new InvitationApproved(
            invitation.Id, invitation.Email, invitation.FullName,
            invitation.TargetRole, approvedById);
    }
}

public static class RejectInvitationHandler
{
    public static async Task<InvitationRejected> Handle(
        RejectInvitation command, IDocumentSession session, ICurrentUserService currentUser)
    {
        var invitation = await session.LoadAsync<UserInvitation>(command.InvitationId);
        if (invitation is null)
            throw new DomainException(SecurityErrors.InvitationNotFound(command.InvitationId));

        if (invitation.Status != InvitationStatus.PendingApproval)
            throw new DomainException(SecurityErrors.InvalidInvitationStatus(
                command.InvitationId, invitation.Status.Slug, InvitationStatus.PendingApproval.Slug));

        // Aktör token'dan gelir, istekten DEĞİL (#137).
        var rejectedById = currentUser.GetUserId();

        invitation.Status = InvitationStatus.Rejected;
        invitation.RejectedAt = DateTime.UtcNow;
        invitation.RejectedById = rejectedById;
        invitation.RejectionReason = command.Reason;
        session.Store(invitation);

        return new InvitationRejected(
            invitation.Id, invitation.Email, invitation.TargetRole,
            rejectedById, command.Reason);
    }
}

public static class CompleteInvitationHandler
{
    public static async Task<object[]> Handle(
        CompleteInvitation command, IKeycloakAdminService keycloak, IDocumentSession session)
    {
        var invitation = await session.LoadAsync<UserInvitation>(command.InvitationId);
        if (invitation is null)
            throw new DomainException(SecurityErrors.InvitationNotFound(command.InvitationId));

        if (invitation.Status != InvitationStatus.Approved)
            throw new DomainException(SecurityErrors.InvitationNotApproved(command.InvitationId));

        if (invitation.ExpiresAt < DateTime.UtcNow)
            throw new DomainException(SecurityErrors.InvitationExpired(command.InvitationId));

        var kcResult = await keycloak.CreateUserAsync(
            command.Username, invitation.Email, invitation.FirstName, invitation.LastName,
            command.Password);

        if (kcResult.IsFailure)
            throw new DomainException(kcResult.Error);

        var keycloakUserId = kcResult.Value;

        var roleResult = await keycloak.AssignRealmRolesAsync(keycloakUserId, [invitation.TargetRole]);
        if (roleResult.IsFailure)
            throw new DomainException(roleResult.Error);

        // institution_id Keycloak'a YAZILMAZ (ADR-0003 adım 2) — CreateUserHandler ile aynı
        // gerekçe: kiracı anahtarının otoritesi UserAccount kaydıdır, Keycloak özniteliği değil.
        var attributes = new Dictionary<string, string>();
        if (invitation.BusinessId.HasValue)
            // business_id Keycloak'a YAZILMAZ (#229) — otorite UserAccount kaydıdır.
            _ = invitation.BusinessId;
        if (attributes.Count > 0)
            await keycloak.SetUserAttributesAsync(keycloakUserId, attributes);

        // Davet metadata'sındaki BranchCode birinci sınıf alana bağlanır (#126) —
        // iki ayrı doğruluk kaynağı bırakılmaz. Metadata yalnız taşıma biçimidir.
        var branchCodes = CreateUserHandler.NormalizeBranchCodes(
            invitation.Metadata.TryGetValue("BranchCode", out var rawBranch)
                ? rawBranch.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                : []);

        if (branchCodes.Count > 0)
        {
            await keycloak.SetUserAttributeValuesAsync(
                keycloakUserId, BranchCodeClaims.ClaimType, branchCodes);
        }

        // Veli–öğrenci bağı (#271). Keycloak özniteliği de kurulur — claim OTORİTER DEĞİLDİR
        // (PermissionClaimsTransformation her istekte kayıttan yeniden yazar), ama öznitelik
        // yoksa kullanıcı yönetimi ekranında bağ görünmez ve tutarsızlık doğar.
        if (invitation.StudentIds.Count > 0)
        {
            await keycloak.SetUserAttributeValuesAsync(
                keycloakUserId, LinkedStudentClaims.ClaimType,
                [.. invitation.StudentIds.Select(id => id.ToString())]);
        }

        var account = new UserAccount
        {
            Id = Guid.NewGuid(),
            KeycloakUserId = keycloakUserId,
            Username = command.Username,
            Email = invitation.Email,
            FirstName = invitation.FirstName,
            LastName = invitation.LastName,
            Roles = [invitation.TargetRole],
            InstitutionId = invitation.InstitutionId,
            BusinessId = invitation.BusinessId,
            BranchCodes = branchCodes,
            LinkedStudentIds = invitation.StudentIds
        };
        session.Store(account);

        invitation.Status = InvitationStatus.Completed;
        invitation.CompletedAt = DateTime.UtcNow;
        invitation.CreatedUserAccountId = account.Id;
        session.Store(invitation);

        var invitationCompleted = new InvitationCompleted(
            invitation.Id, account.Id, keycloakUserId, account.FullName, invitation.TargetRole);

        var userCreated = new UserCreated(
            account.Id, keycloakUserId, command.Username, account.FullName, invitation.Email,
            [invitation.TargetRole], invitation.InstitutionId, invitation.BusinessId,
            invitation.Metadata);

        // Denetim satırları yalnız kullanıcı kimliğini saklar; adı modüller bu olayla
        // besledikleri kendi UserNameView'larından çözer (#137).
        return UserDisplayNameEvents.TryCreate(account) is { } displayName
            ? [invitationCompleted, userCreated, displayName]
            : [invitationCompleted, userCreated];
    }
}

public static class GetInvitationsHandler
{
    public static async Task<PagedResult<InvitationDto>> Handle(
        GetInvitations query, IQuerySession session,
        UserScopeResolver scopeResolver, CancellationToken cancellationToken)
    {
        IQueryable<UserInvitation> queryable = session.Query<UserInvitation>();

        // KAPSAM — kullanıcı listesiyle AYNI kapıdan. Kurum bağı olmayan davetler bilerek
        // GÖRÜNÜR KALIR (yüklemdeki `== null` dalı): CreateInvitation InstitutionId'yi isteğe
        // bağlı alır ve süzülüp düşen davet onaylanamaz/reddedilemez hâle gelirdi.
        var visibleIds = await scopeResolver.ResolveAsync(cancellationToken);
        if (visibleIds is { } ids)
            queryable = queryable.Where(i => i.InstitutionId == null || ids.Contains(i.InstitutionId.Value));

        if (query.InstitutionId.HasValue)
            queryable = queryable.Where(i => i.InstitutionId == query.InstitutionId.Value);

        if (!string.IsNullOrEmpty(query.TargetRole))
            queryable = queryable.Where(i => i.TargetRole == query.TargetRole);

        if (!string.IsNullOrEmpty(query.Status) && InvitationStatus.TryFromName(query.Status, true, out var status))
            queryable = queryable.Where(i => i.StatusName == status.Name);

        queryable = queryable.ApplySearch(query.Search, i => i.Email, i => i.FullName);
        queryable = queryable.ApplySort(query.SortBy, query.Descending, defaultSort: i => i.CreatedAt);

        var page = await queryable.ToPagedResultAsync(query, i => i);

        // Aktör adı saklanmaz, okuma anında çözülür (#137). Security kendi kimlik kaynağıdır;
        // UserAccount aynı modülde olduğu için ayrı UserNameView'a gerek yoktur.
        var names = await ResolveActorNamesAsync(session, page.Items);

        return new PagedResult<InvitationDto>
        {
            Items = [.. page.Items.Select(i => new InvitationDto(
                i.Id, i.Email, i.FirstName, i.LastName, i.FullName,
                i.TargetRole, i.Status.ToString(), i.InstitutionId, i.BusinessId,
                i.CreatedAt, i.CreatedById, NameOf(names, i.CreatedById),
                i.ApprovedAt, i.ApprovedById, NameOf(names, i.ApprovedById),
                i.ExpiresAt))],
            TotalCount = page.TotalCount,
            Page = page.Page,
            PageSize = page.PageSize
        };
    }

    /// <summary>Sayfadaki tüm aktör kimlikleri için kimlik → ad sözlüğü (tek sorgu).</summary>
    private static async Task<Dictionary<Guid, string>> ResolveActorNamesAsync(
        IQuerySession session, IReadOnlyList<UserInvitation> invitations)
    {
        var ids = invitations
            .SelectMany(i => new[] { i.CreatedById, i.ApprovedById })
            .Where(id => id is { } value && value != Guid.Empty)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        if (ids.Length == 0) return [];

        // UserAccount.Id ile değil KeycloakUserId ile eşleşir: denetim alanı token'ın
        // `sub` claim'ini saklar, lokal hesap kimliğini değil.
        var idStrings = ids.Select(id => id.ToString()).ToArray();

        var accounts = await session.Query<UserAccount>()
            .Where(a => idStrings.Contains(a.KeycloakUserId))
            .ToListAsync();

        return accounts
            .Where(a => Guid.TryParse(a.KeycloakUserId, out _))
            .ToDictionary(a => Guid.Parse(a.KeycloakUserId), a => a.FullName);
    }

    private static string? NameOf(Dictionary<Guid, string> names, Guid? userId) =>
        userId is { } id && id != Guid.Empty && names.TryGetValue(id, out var name) ? name : null;
}

public static class ResendInvitationHandler
{
    public static async Task Handle(
        ResendInvitation command, IDocumentSession session, IEmailService emailService)
    {
        var invitation = await session.LoadAsync<UserInvitation>(command.InvitationId);
        if (invitation is null)
            throw new DomainException(SecurityErrors.InvitationNotFound(command.InvitationId));

        if (invitation.Status != InvitationStatus.Approved)
            throw new DomainException(SecurityErrors.InvitationNotApproved(command.InvitationId));

        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            invitation.ExpiresAt = DateTime.UtcNow.AddDays(7);
            session.Store(invitation);
        }

        await emailService.SendInvitationEmailAsync(
            invitation.Email, invitation.FullName, invitation.TargetRole, invitation.Id);
    }
}

/// <summary>
/// Davet listesi satırı.
///
/// <para><b><c>Metadata</c> bilerek YOKTUR.</b> Öğrenci davetinde T.C. kimlik numarası
/// taşıyordu ve liste ucu onu kendi okulunun her davetini gören herkese veriyordu. Bu bir
/// veri minimizasyonu kararıdır ve kurum kapsamından BAĞIMSIZDIR — kapsam daraltılsa bile
/// alan gerekmiyordu. Tüketicisi ölçüldü: ön yüz onu yalnız davet OLUŞTURURKEN gönderiyor,
/// listede hiç okumuyor.</para>
/// </summary>
/// <remarks>
/// Aktör alanları hem kimlik hem çözümlenmiş ad taşır (#137): kimlik saklanan değerdir,
/// ad okuma anında türetilir ve bilinmiyorsa <c>null</c> olur (silinmiş kullanıcı vb.).
/// </remarks>
public sealed record InvitationDto(
    Guid Id, string Email, string FirstName, string LastName, string FullName,
    string TargetRole, string Status, Guid? InstitutionId, Guid? BusinessId,
    DateTime CreatedAt, Guid? CreatedById, string? CreatedByName,
    DateTime? ApprovedAt, Guid? ApprovedById, string? ApprovedByName,
    DateTime ExpiresAt);
