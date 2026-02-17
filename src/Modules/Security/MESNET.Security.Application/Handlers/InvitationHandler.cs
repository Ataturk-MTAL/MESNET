using Marten;
using MESNET.Common.Shared;
using MESNET.Security.Application.Commands;
using MESNET.Security.Application.Errors;
using MESNET.Security.Application.Services;
using MESNET.Security.Core.Entities;
using MESNET.Security.Core.Enums;
using MESNET.Security.Shared.Events;

namespace MESNET.Security.Application.Handlers;

public static class CreateInvitationHandler
{
    public static async Task<(Result<Guid>, InvitationCreated?)> Handle(
        CreateInvitation command, IDocumentSession session)
    {
        // Aynı email + aynı rol + aktif davet var mı kontrol
        var existing = await session.Query<UserInvitation>()
            .Where(i => i.Email == command.Email
                        && i.TargetRole == command.TargetRole
                        && (i.Status == InvitationStatus.PendingApproval || i.Status == InvitationStatus.Approved))
            .FirstOrDefaultAsync();

        if (existing is not null)
            return (Result<Guid>.Failure(SecurityErrors.InvitationAlreadyExists(command.Email, command.TargetRole)), null);

        var invitation = new UserInvitation
        {
            Id = Guid.NewGuid(),
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            TargetRole = command.TargetRole,
            InstitutionId = command.InstitutionId,
            BusinessId = command.BusinessId,
            CreatedByName = command.CreatedByName,
            Metadata = command.Metadata ?? [],
            Status = InvitationStatus.PendingApproval
        };

        session.Store(invitation);

        var @event = new InvitationCreated(
            invitation.Id, command.Email, invitation.FullName,
            command.TargetRole, command.InstitutionId, command.BusinessId);

        return (Result<Guid>.Success(invitation.Id), @event);
    }
}

public static class ApproveInvitationHandler
{
    public static async Task<(Result, InvitationApproved?)> Handle(
        ApproveInvitation command, IDocumentSession session, IEmailService emailService)
    {
        var invitation = await session.LoadAsync<UserInvitation>(command.InvitationId);
        if (invitation is null)
            return (Result.Failure(SecurityErrors.InvitationNotFound(command.InvitationId)), null);

        if (invitation.Status != InvitationStatus.PendingApproval)
            return (Result.Failure(SecurityErrors.InvalidInvitationStatus(
                command.InvitationId, invitation.Status.ToString(), InvitationStatus.PendingApproval.ToString())), null);

        invitation.Status = InvitationStatus.Approved;
        invitation.ApprovedAt = DateTime.UtcNow;
        invitation.ApprovedByName = command.ApprovedByName;
        invitation.ExpiresAt = DateTime.UtcNow.AddDays(7);
        session.Store(invitation);

        // Email ile kayıt bağlantısı gönder
        await emailService.SendInvitationEmailAsync(
            invitation.Email, invitation.FullName, invitation.TargetRole, invitation.Id);

        return (Result.Success(), new InvitationApproved(
            invitation.Id, invitation.Email, invitation.FullName,
            invitation.TargetRole, command.ApprovedByName));
    }
}

public static class RejectInvitationHandler
{
    public static async Task<(Result, InvitationRejected?)> Handle(
        RejectInvitation command, IDocumentSession session)
    {
        var invitation = await session.LoadAsync<UserInvitation>(command.InvitationId);
        if (invitation is null)
            return (Result.Failure(SecurityErrors.InvitationNotFound(command.InvitationId)), null);

        if (invitation.Status != InvitationStatus.PendingApproval)
            return (Result.Failure(SecurityErrors.InvalidInvitationStatus(
                command.InvitationId, invitation.Status.ToString(), InvitationStatus.PendingApproval.ToString())), null);

        invitation.Status = InvitationStatus.Rejected;
        invitation.RejectedAt = DateTime.UtcNow;
        invitation.RejectedByName = command.RejectedByName;
        invitation.RejectionReason = command.Reason;
        session.Store(invitation);

        return (Result.Success(), new InvitationRejected(
            invitation.Id, invitation.Email, invitation.TargetRole,
            command.RejectedByName, command.Reason));
    }
}

public static class CompleteInvitationHandler
{
    public static async Task<(Result, object[]?)> Handle(
        CompleteInvitation command, IKeycloakAdminService keycloak, IDocumentSession session)
    {
        var invitation = await session.LoadAsync<UserInvitation>(command.InvitationId);
        if (invitation is null)
            return (Result.Failure(SecurityErrors.InvitationNotFound(command.InvitationId)), null);

        if (invitation.Status != InvitationStatus.Approved)
            return (Result.Failure(SecurityErrors.InvitationNotApproved(command.InvitationId)), null);

        if (invitation.ExpiresAt < DateTime.UtcNow)
            return (Result.Failure(SecurityErrors.InvitationExpired(command.InvitationId)), null);

        // 1. Keycloak'ta kullanıcı oluştur
        var kcResult = await keycloak.CreateUserAsync(
            command.Username, invitation.Email, invitation.FirstName, invitation.LastName,
            command.Password);

        if (kcResult.IsFailure)
            return (kcResult, null);

        var keycloakUserId = kcResult.Value;

        // 2. Keycloak'ta rol ata
        var roleResult = await keycloak.AssignRealmRolesAsync(keycloakUserId, [invitation.TargetRole]);
        if (roleResult.IsFailure)
            return (roleResult, null);

        // 3. User attributes ata
        var attributes = new Dictionary<string, string>();
        if (invitation.InstitutionId.HasValue)
            attributes["institution_id"] = invitation.InstitutionId.Value.ToString();
        if (invitation.BusinessId.HasValue)
            attributes["business_id"] = invitation.BusinessId.Value.ToString();
        if (attributes.Count > 0)
            await keycloak.SetUserAttributesAsync(keycloakUserId, attributes);

        // 4. UserAccount oluştur
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
            BusinessId = invitation.BusinessId
        };
        session.Store(account);

        // 5. Davet tamamla
        invitation.Status = InvitationStatus.Completed;
        invitation.CompletedAt = DateTime.UtcNow;
        invitation.CreatedUserAccountId = account.Id;
        session.Store(invitation);

        // 6. Cascading event'ler
        var invitationCompleted = new InvitationCompleted(
            invitation.Id, account.Id, keycloakUserId, account.FullName, invitation.TargetRole);

        var userCreated = new UserCreated(
            account.Id, keycloakUserId, command.Username, account.FullName, invitation.Email,
            [invitation.TargetRole], invitation.InstitutionId, invitation.BusinessId,
            invitation.Metadata);

        return (Result.Success(), new object[] { invitationCompleted, userCreated });
    }
}

public static class GetInvitationsHandler
{
    public static async Task<IReadOnlyList<InvitationDto>> Handle(
        GetInvitations query, IQuerySession session)
    {
        IQueryable<UserInvitation> queryable = session.Query<UserInvitation>();

        if (query.InstitutionId.HasValue)
            queryable = queryable.Where(i => i.InstitutionId == query.InstitutionId.Value);

        if (!string.IsNullOrEmpty(query.TargetRole))
            queryable = queryable.Where(i => i.TargetRole == query.TargetRole);

        if (!string.IsNullOrEmpty(query.Status) && Enum.TryParse<InvitationStatus>(query.Status, true, out var status))
            queryable = queryable.Where(i => i.Status == status);

        var invitations = await queryable
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return invitations.Select(i => new InvitationDto(
            i.Id, i.Email, i.FirstName, i.LastName, i.FullName,
            i.TargetRole, i.Status.ToString(), i.InstitutionId, i.BusinessId,
            i.CreatedAt, i.CreatedByName, i.ApprovedAt, i.ApprovedByName,
            i.ExpiresAt, i.Metadata)).ToList();
    }
}

public static class ResendInvitationHandler
{
    public static async Task<Result> Handle(
        ResendInvitation command, IDocumentSession session, IEmailService emailService)
    {
        var invitation = await session.LoadAsync<UserInvitation>(command.InvitationId);
        if (invitation is null)
            return Result.Failure(SecurityErrors.InvitationNotFound(command.InvitationId));

        if (invitation.Status != InvitationStatus.Approved)
            return Result.Failure(SecurityErrors.InvitationNotApproved(command.InvitationId));

        // Süre dolmuşsa yenile
        if (invitation.ExpiresAt < DateTime.UtcNow)
        {
            invitation.ExpiresAt = DateTime.UtcNow.AddDays(7);
            session.Store(invitation);
        }

        await emailService.SendInvitationEmailAsync(
            invitation.Email, invitation.FullName, invitation.TargetRole, invitation.Id);

        return Result.Success();
    }
}

public sealed record InvitationDto(
    Guid Id, string Email, string FirstName, string LastName, string FullName,
    string TargetRole, string Status, Guid? InstitutionId, Guid? BusinessId,
    DateTime CreatedAt, string? CreatedByName, DateTime? ApprovedAt, string? ApprovedByName,
    DateTime ExpiresAt, Dictionary<string, string> Metadata);
