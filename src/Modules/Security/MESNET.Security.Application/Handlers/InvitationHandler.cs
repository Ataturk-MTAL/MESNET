using Marten;
using MESNET.Common.Infrastructure.Email;
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
    public static async Task<(Guid, InvitationCreated)> Handle(
        CreateInvitation command, IDocumentSession session)
    {
        var existing = await session.Query<UserInvitation>()
            .Where(i => i.Email == command.Email
                        && i.TargetRole == command.TargetRole
                        && (i.Status == InvitationStatus.PendingApproval || i.Status == InvitationStatus.Approved))
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
            CreatedByName = command.CreatedByName,
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
        ApproveInvitation command, IDocumentSession session, IEmailService emailService)
    {
        var invitation = await session.LoadAsync<UserInvitation>(command.InvitationId);
        if (invitation is null)
            throw new DomainException(SecurityErrors.InvitationNotFound(command.InvitationId));

        if (invitation.Status != InvitationStatus.PendingApproval)
            throw new DomainException(SecurityErrors.InvalidInvitationStatus(
                command.InvitationId, invitation.Status.ToString(), InvitationStatus.PendingApproval.ToString()));

        invitation.Status = InvitationStatus.Approved;
        invitation.ApprovedAt = DateTime.UtcNow;
        invitation.ApprovedByName = command.ApprovedByName;
        invitation.ExpiresAt = DateTime.UtcNow.AddDays(7);
        session.Store(invitation);

        await emailService.SendInvitationEmailAsync(
            invitation.Email, invitation.FullName, invitation.TargetRole, invitation.Id);

        return new InvitationApproved(
            invitation.Id, invitation.Email, invitation.FullName,
            invitation.TargetRole, command.ApprovedByName);
    }
}

public static class RejectInvitationHandler
{
    public static async Task<InvitationRejected> Handle(
        RejectInvitation command, IDocumentSession session)
    {
        var invitation = await session.LoadAsync<UserInvitation>(command.InvitationId);
        if (invitation is null)
            throw new DomainException(SecurityErrors.InvitationNotFound(command.InvitationId));

        if (invitation.Status != InvitationStatus.PendingApproval)
            throw new DomainException(SecurityErrors.InvalidInvitationStatus(
                command.InvitationId, invitation.Status.ToString(), InvitationStatus.PendingApproval.ToString()));

        invitation.Status = InvitationStatus.Rejected;
        invitation.RejectedAt = DateTime.UtcNow;
        invitation.RejectedByName = command.RejectedByName;
        invitation.RejectionReason = command.Reason;
        session.Store(invitation);

        return new InvitationRejected(
            invitation.Id, invitation.Email, invitation.TargetRole,
            command.RejectedByName, command.Reason);
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

        var attributes = new Dictionary<string, string>();
        if (invitation.InstitutionId.HasValue)
            attributes["institution_id"] = invitation.InstitutionId.Value.ToString();
        if (invitation.BusinessId.HasValue)
            attributes["business_id"] = invitation.BusinessId.Value.ToString();
        if (attributes.Count > 0)
            await keycloak.SetUserAttributesAsync(keycloakUserId, attributes);

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

        return [invitationCompleted, userCreated];
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

public sealed record InvitationDto(
    Guid Id, string Email, string FirstName, string LastName, string FullName,
    string TargetRole, string Status, Guid? InstitutionId, Guid? BusinessId,
    DateTime CreatedAt, string? CreatedByName, DateTime? ApprovedAt, string? ApprovedByName,
    DateTime ExpiresAt, Dictionary<string, string> Metadata);
